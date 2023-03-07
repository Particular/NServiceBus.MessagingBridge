using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.Faults;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Retry : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_work()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<ProcessingEndpoint>(builder =>
            {
                builder.DoNotFailOnErrorMessages();
                builder.When(c => c.EndpointsStarted, (session, _) => session.SendLocal(new FaultyMessage()));
            })
            .WithEndpoint<FakeSCError>()
            .WithEndpoint<FakeSCAudit>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(DefaultTestServer.GetTestTransportDefinition())
                {
                    Name = "DefaultTestingTransport"
                };
                bridgeTransport.AddTestEndpoint<FakeSCError>();
                bridgeTransport.AddTestEndpoint<FakeSCAudit>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var processingEndpoint =
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));

                var theOtherTransport = new TestableBridgeTransport(TransportBeingTested);
                theOtherTransport.HasEndpoint(processingEndpoint);
                bridgeConfiguration.AddTransport(theOtherTransport);
            })
            .Done(c => c.MessageAudited)
            .Run();

        Assert.IsTrue(ctx.MessageFailed);
        Assert.IsTrue(ctx.RetryDelivered);
        Assert.IsTrue(ctx.GotRetrySuccessfullAck);
        Assert.IsTrue(ctx.MessageAudited);

        foreach (var header in ctx.FailedMessageHeaders)
        {
            if (ctx.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
            {
                Assert.AreEqual(header.Value, receivedHeaderValue,
                    $"{header.Key} is not the same on processed message and message sent to the error queue");
            }
        }
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(FakeSCError)));
            c.AuditProcessedMessagesTo(Conventions.EndpointNamingConvention(typeof(FakeSCAudit)));
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

    public class FakeSCAudit : EndpointConfigurationBuilder
    {
        public FakeSCAudit() => EndpointSetup<DefaultTestServer>();

        class FailedMessageHander : IHandleMessages<FaultyMessage>
        {
            public FailedMessageHander(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.MessageAudited = true;

                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> AuditMessageHeaders { get; set; }
        public bool RetryDelivered { get; set; }
        public bool GotRetrySuccessfullAck { get; set; }
        public bool MessageAudited { get; set; }
    }

    public class FaultyMessage : IMessage
    {
    }
}