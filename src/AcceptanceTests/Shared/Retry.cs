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
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                if (doNotTranslateReplyToAdressForFailedMessages)
                {
                    bridgeConfiguration.DoNotTranslateReplyToAddressForFailedMessages();
                }

                var bridgeTransport = DefaultTestServer.GetTestTransportDefinition().ToTestableBridge("DefaultTestingTransport");
                bridgeTransport.AddTestEndpoint<FakeSCError>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                transportBeingTested.AddTestEndpoint<ProcessingEndpoint>();
                bridgeConfiguration.AddTransport(transportBeingTested);
            })
            .WithEndpoint<ProcessingEndpoint>(b => b.When(session => session.SendLocal(new FaultyMessage()))
                .DoNotFailOnErrorMessages())
            .WithEndpoint<FakeSCError>()
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.MessageFailed, Is.True);
            Assert.That(context.RetryDelivered, Is.True);
        });

        foreach (var header in context.FailedMessageHeaders)
        {
            if (context.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
            {
                if (!doNotTranslateReplyToAdressForFailedMessages && header.Key == Headers.ReplyToAddress)
                {
                    Assert.That(receivedHeaderValue.Contains(nameof(ProcessingEndpoint), StringComparison.InvariantCultureIgnoreCase), Is.True,
                        $"The ReplyToAddress received by ServiceControl ({context.TransportBeingTested} physical address) should contain the logical name of the endpoint.");
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

        public class MessageHandler(Context testContext) : IHandleMessages<FaultyMessage>
        {
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

        class FailedMessageHandler(Context testContext) : IHandleMessages<FaultyMessage>
        {
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
        }

        class ControlMessageBehavior(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                if (context.MessageHeaders.ContainsKey("ServiceControl.Retry.Successful"))
                {
                    testContext.MarkAsCompleted();
                    return;
                }
                await next();

            }
        }
    }

    public class Context : BridgeScenarioContext
    {
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
        public bool RetryDelivered { get; set; }
    }

    public class FaultyMessage : IMessage;
}