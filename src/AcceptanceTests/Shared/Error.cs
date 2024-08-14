using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Error : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_forward_error_messages_by_not_modify_message()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<PublishingEndpoint>(b => b
                .When(c => TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, c) =>
                {
                    return session.Publish(new FaultyMessage());
                }))
            .WithEndpoint<ProcessingEndpoint>(builder => builder.DoNotFailOnErrorMessages())
            .WithEndpoint<ErrorSpy>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);
                bridgeTransport.AddTestEndpoint<PublishingEndpoint>();
                bridgeTransport.AddTestEndpoint<ErrorSpy>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint =
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));
                subscriberEndpoint.RegisterPublisher<FaultyMessage>(
                    Conventions.EndpointNamingConvention(typeof(PublishingEndpoint)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);

            })
            .Done(c => c.MessageFailed)
            .Run();

        Assert.That(ctx.MessageFailed, Is.True);
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
            EndpointSetup<DefaultServer>(c =>
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
        public ProcessingEndpoint() => EndpointSetup<DefaultTestServer>(
            c => c.SendFailedMessagesTo("Error.ErrorSpy"));

        public class MessageHandler : IHandleMessages<FaultyMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                throw new Exception("Simulated");
            }
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy()
        {
            var endpoint = EndpointSetup<DefaultServer>(c => c.AutoSubscribe().DisableFor<FaultyMessage>());
        }

        class FailedMessageHander : IHandleMessages<FaultyMessage>
        {
            public FailedMessageHander(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.FailedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                testContext.MessageFailed = true;

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
    }

    public class FaultyMessage : IEvent
    {
    }
}