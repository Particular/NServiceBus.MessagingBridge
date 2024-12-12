using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Faults;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Retry : BridgeAcceptanceTest
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_work(bool doNotTranslateReplyToAdressForFailedMessages)
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<ProcessingEndpoint>(builder =>
            {
                builder.DoNotFailOnErrorMessages();
                builder.When(c => c.EndpointsStarted, (session, _) => session.SendLocal(new FaultyMessage()));
            })
            .WithEndpoint<FakeSCError>()
            .WithBridge(bridgeConfiguration =>
            {
                if (doNotTranslateReplyToAdressForFailedMessages)
                {
                    bridgeConfiguration.DoNotTranslateReplyToAddressForFailedMessages();
                }
                var bridgeTransport = new TestableBridgeTransport(DefaultTestServer.GetTestTransportDefinition())
                {
                    Name = "DefaultTestingTransport"
                };
                bridgeTransport.AddTestEndpoint<FakeSCError>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var theOtherTransport = new TestableBridgeTransport(TransportBeingTested);
                theOtherTransport.AddTestEndpoint<ProcessingEndpoint>();
                bridgeConfiguration.AddTransport(theOtherTransport);
            })
            .Done(c => c.GotRetrySuccessfullAck)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(ctx.MessageFailed, Is.True);
            Assert.That(ctx.RetryDelivered, Is.True);
            Assert.That(ctx.GotRetrySuccessfullAck, Is.True);
        });

        foreach (var header in ctx.FailedMessageHeaders)
        {
            if (ctx.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
            {
                if (!doNotTranslateReplyToAdressForFailedMessages && header.Key == Headers.ReplyToAddress)
                {
                    Assert.That(receivedHeaderValue.Contains(nameof(ProcessingEndpoint), StringComparison.InvariantCultureIgnoreCase), Is.True,
                        $"The ReplyToAddress received by ServiceControl ({TransportBeingTested} physical address) should contain the logical name of the endpoint.");
                }
                else
                {
                    Assert.That(receivedHeaderValue, Is.EqualTo(header.Value),
                    $"{header.Key} is not the same on processed message and message sent to the error queue");
                }
            }
        }
    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(FakeSCError)));
            c.ConfigureRouting().RouteToEndpoint(typeof(FaultyMessage), Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));
        });
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(FakeSCError)));
        });

        public class MessageHandler : IHandleMessages<FaultyMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                if (testContext.MessageFailed)
                {
                    testContext.RetryDelivered = true;
                    return Task.CompletedTask;
                }

                testContext.ReceivedMessageHeaders =
                new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                throw new Exception("Simulated");
            }
        }
    }

    public class FakeSCError : EndpointConfigurationBuilder
    {
        public FakeSCError() => EndpointSetup<DefaultTestServer>((c, runDescriptor) =>
                c.Pipeline.Register(new ControlMessageBehavior(runDescriptor.ScenarioContext as Context), "Checks that the retry confirmation arrived"));

        class FailedMessageHander : IHandleMessages<FaultyMessage>
        {
            public FailedMessageHander(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.FailedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                testContext.MessageFailed = true;

                var sendOptions = new SendOptions();

                //Send the message to the FailedQ address
                string destination = context.MessageHeaders[FaultsHeaderKeys.FailedQ];
                sendOptions.SetDestination(destination);

                //ServiceControl adds these headers when retrying
                sendOptions.SetHeader("ServiceControl.Retry.UniqueMessageId", "some-id");
                sendOptions.SetHeader("ServiceControl.Retry.AcknowledgementQueue", Conventions.EndpointNamingConvention(typeof(FakeSCError)));
                return context.Send(new FaultyMessage(), sendOptions);
            }

            readonly Context testContext;
        }

        class ControlMessageBehavior : Behavior<IIncomingPhysicalMessageContext>
        {
            public ControlMessageBehavior(Context testContext) => this.testContext = testContext;

            public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                if (context.MessageHeaders.ContainsKey("ServiceControl.Retry.Successful"))
                {
                    testContext.GotRetrySuccessfullAck = true;
                    return;
                }
                await next();

            }

            Context testContext;
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
        public bool RetryDelivered { get; set; }
        public bool GotRetrySuccessfullAck { get; set; }
    }

    public class FaultyMessage : IMessage
    {
    }
}