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
    public async Task Should_forward_retry_messages()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<PublishingEndpoint>(b => b
                .When(c => c.EndpointsStarted, (session, c) =>
                {
                    return session.Publish(new FaultyMessage());
                }))
            .WithEndpoint<ProcessingEndpoint>(builder => builder.DoNotFailOnErrorMessages())
            .WithEndpoint<ErrorSpy>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(DefaultTestServer.GetTestTransportDefinition())
                {
                    Name = "DefaultTestingTransport"
                };
                bridgeTransport.AddTestEndpoint<PublishingEndpoint>();
                bridgeTransport.AddTestEndpoint<ErrorSpy>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint =
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));
                subscriberEndpoint.RegisterPublisher<FaultyMessage>(
                    Conventions.EndpointNamingConvention(typeof(PublishingEndpoint)));

                var theOtherTransport = new TestableBridgeTransport(TransportBeingTested);
                theOtherTransport.HasEndpoint(subscriberEndpoint);
                bridgeConfiguration.AddTransport(theOtherTransport);
            })
            .Done(c => c.GetRetrySuccessfullAck)
            .Run();

        Assert.IsTrue(ctx.RetryDelivered);
        Assert.IsTrue(ctx.MessageFailed);

        foreach (var header in ctx.FailedMessageHeaders)
        {
            if (ctx.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
            {
                Assert.AreEqual(header.Value, receivedHeaderValue,
                    $"{header.Key} is not the same on processed message and audit message.");
            }
        }
    }

    public class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() =>
            EndpointSetup<DefaultTestPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
                c.ConfigureRouting().RouteToEndpoint(typeof(FaultyMessage), typeof(ProcessingEndpoint));
            });
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultServer>(
            c => c.SendFailedMessagesTo("Retry.ErrorSpy"));

        public class MessageHandler : IHandleMessages<FaultyMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                testContext.MessageFailed = true;

                throw new Exception("Simulated");
            }
        }

        public class RetryMessageHandler : IHandleMessages<RetryMessage>
        {
            readonly Context testContext;

            public RetryMessageHandler(Context context) => testContext = context;

            public Task Handle(RetryMessage message, IMessageHandlerContext context)
            {
                testContext.RetryDelivered = true;

                return Task.CompletedTask;
            }
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy()
        {
            var endpoint = EndpointSetup<DefaultTestServer>((c, runDescriptor) =>
            {
                c.AutoSubscribe().DisableFor<FaultyMessage>();
                c.Pipeline.Register(new ControlMessageBehavior(runDescriptor.ScenarioContext as Context), "Checks that the retry confirmation arrived");
            });
        }

        class FailedMessageHander : IHandleMessages<FaultyMessage>
        {
            public FailedMessageHander(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.FailedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                var sendOptions = new SendOptions();

                //Send the message to the FailedQ address
                string destination = context.MessageHeaders[FaultsHeaderKeys.FailedQ];
                sendOptions.SetDestination(destination);

                //ServiceControl adds these headers when retrying
                sendOptions.SetHeader("ServiceControl.Retry.UniqueMessageId", "XYZ");
                sendOptions.SetHeader("ServiceControl.Retry.AcknowledgementQueue", "Retry.ErrorSpy");
                return context.Send(new RetryMessage(), sendOptions);
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
                    testContext.GetRetrySuccessfullAck = true;
                    return;
                }
                await next();

            }

            Context testContext;
        }
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
        public bool RetryDelivered { get; set; }
        public bool GetRetrySuccessfullAck { get; internal set; }
    }

    public class FaultyMessage : IEvent
    {
    }

    public class RetryMessage : IMessage
    {
    }
}