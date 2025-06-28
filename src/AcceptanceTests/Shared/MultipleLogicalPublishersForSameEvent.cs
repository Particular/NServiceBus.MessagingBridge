using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class MultipleLogicalPublishersForSameEvent : BridgeAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge(bridgeConfiguration =>
            {
                bridgeConfiguration.DoNotEnforceBestPractices();

                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                bridgeTransport.AddTestEndpoint<PublisherOne>();
                bridgeTransport.AddTestEndpoint<PublisherTwo>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(PublisherOne)));
                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(PublisherTwo)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            })
            .WithEndpoint<PublisherOne>(b => b.When(ctx => ctx.SubscriberPublisherOneSubscribed, (session, _) => session.Publish(new MyEvent())))
            .WithEndpoint<PublisherTwo>(b => b.When(ctx => ctx.SubscriberPublisherTwoSubscribed, (session, _) => session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>(b => b.When(async (session, c) =>
            {
                await session.Subscribe<MyEvent>();
                if (c.HasNativePubSubSupport)
                {
                    c.SubscriberPublisherOneSubscribed = true;
                    c.SubscriberPublisherTwoSubscribed = true;
                }
            }))
            .Done(c => c.SubscriberGotEventFromPublisherOne && c.SubscriberGotEventFromPublisherTwo)
            .Run();
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberPublisherOneSubscribed { get; set; }
        public bool SubscriberPublisherTwoSubscribed { get; set; }
        public bool SubscriberGotEventFromPublisherOne { get; set; }
        public bool SubscriberGotEventFromPublisherTwo { get; set; }
    }

    class PublisherOne : EndpointConfigurationBuilder
    {
        public PublisherOne() =>
            EndpointSetup<DefaultPublisher>(
                b => b.OnEndpointSubscribed<Context>((s, ctx) =>
                {
                    var subscriber = Conventions.EndpointNamingConvention(typeof(Subscriber));
                    if (s.SubscriberEndpoint.Contains(subscriber))
                    {
                        ctx.SubscriberPublisherOneSubscribed = true;
                    }
                }),
                metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    class PublisherTwo : EndpointConfigurationBuilder
    {
        public PublisherTwo() =>
            EndpointSetup<DefaultPublisher>(
                b => b.OnEndpointSubscribed<Context>((s, ctx) =>
                {
                    var subscriber = Conventions.EndpointNamingConvention(typeof(Subscriber));
                    if (s.SubscriberEndpoint.Contains(subscriber))
                    {
                        ctx.SubscriberPublisherTwoSubscribed = true;
                    }
                }),
                metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultTestServer>(b => b.DisableFeature<AutoSubscribe>(), metadata =>
            {
                metadata.RegisterPublisherFor<MyEvent, PublisherOne>();
                metadata.RegisterPublisherFor<MyEvent, PublisherTwo>();
            });

        public class MessageHandler(Context context) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
            {
                string originatingEndpoint = handlerContext.MessageHeaders[Headers.OriginatingEndpoint];
                switch (originatingEndpoint)
                {
                    case "Multiplelogicalpublishersforsameevent.PublisherOne":
                        context.SubscriberGotEventFromPublisherOne = true;
                        break;
                    case "Multiplelogicalpublishersforsameevent.PublisherTwo":
                        context.SubscriberGotEventFromPublisherTwo = true;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown originating endpoint: {originatingEndpoint}");
                }

                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}