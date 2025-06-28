using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Error : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_forward_error_messages_and_not_modify_header_other_than_ReplyToAddress()
    {
        var context = await Scenario.Define<Context>()
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
            .WithEndpoint<PublishingEndpoint>(b => b
                .When(ctx => ctx.SubscriberSubscribed, (session, _) => session.Publish(new FaultyMessage())))
            .WithEndpoint<ProcessingEndpoint>(b => b.When(async (session, c) =>
            {
                await session.Subscribe<FaultyMessage>();
                if (c.HasNativePubSubSupport)
                {
                    c.SubscriberSubscribed = true;
                }
            }).DoNotFailOnErrorMessages())
            .WithEndpoint<ErrorSpy>()
            .Done(c => c.MessageFailed)
            .Run();

        Assert.That(context.MessageFailed, Is.True);
        foreach (var header in context.FailedMessageHeaders)
        {
            if (header.Key != Headers.ReplyToAddress)
            {
                if (context.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
                {
                    Assert.That(receivedHeaderValue, Is.EqualTo(header.Value),
                        $"{header.Key} is not the same on processed message and error message.");
                }
            }
        }
    }

    public class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() =>
            EndpointSetup<DefaultServer>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, ctx) =>
                {
                    var subscriber = Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint));
                    if (s.SubscriberEndpoint.Contains(subscriber))
                    {
                        ctx.SubscriberSubscribed = true;
                    }
                });
                b.ConfigureRouting().RouteToEndpoint(typeof(FaultyMessage), typeof(ProcessingEndpoint));
            }, metadata => metadata.RegisterSelfAsPublisherFor<FaultyMessage>(this));
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultTestServer>(
            b =>
            {
                b.DisableFeature<AutoSubscribe>();
                b.SendFailedMessagesTo("Error.ErrorSpy");
            }, metadata => metadata.RegisterPublisherFor<FaultyMessage, PublishingEndpoint>());

        public class MessageHandler(Context testContext) : IHandleMessages<FaultyMessage>
        {
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
        public ErrorSpy() => EndpointSetup<DefaultServer>(c => c.AutoSubscribe().DisableFor<FaultyMessage>());

        class FailedMessageHandler(Context testContext) : IHandleMessages<FaultyMessage>
        {
            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.FailedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                testContext.MessageFailed = true;

                return Task.CompletedTask;
            }
        }
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
    }

    public class FaultyMessage : IEvent;
}