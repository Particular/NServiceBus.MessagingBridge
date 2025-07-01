using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class MultipleLogicalPublishersForSameEvent : BridgeAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                bridgeConfiguration.DoNotEnforceBestPractices();

                transportBeingTested.AddTestEndpoint<PublisherOne>();
                transportBeingTested.AddTestEndpoint<PublisherTwo>();
                bridgeConfiguration.AddTransport(transportBeingTested);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(PublisherOne)));
                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(PublisherTwo)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            }, metadata =>
            {
                metadata.RegisterPublisherFor<MyEvent, PublisherOne>();
                metadata.RegisterPublisherFor<MyEvent, PublisherTwo>();
            })
            .WithEndpoint<PublisherOne>(b => b
                .When(c => c.TransportBeingTested.SupportsPublishSubscribe || c.SubscriberPublisherOneSubscribed, (session, _) => session.Publish(new MyEvent())))
            .WithEndpoint<PublisherTwo>(b => b
                .When(c => c.TransportBeingTested.SupportsPublishSubscribe || c.SubscriberPublisherTwoSubscribed, (session, c) => session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotEventFromPublisherOne && c.SubscriberGotEventFromPublisherTwo)
            .Run();
    }

    public class Context : BridgeScenarioContext
    {
        public bool SubscriberPublisherOneSubscribed { get; set; }
        public bool SubscriberPublisherTwoSubscribed { get; set; }
        public bool SubscriberGotEventFromPublisherOne { get; set; }
        public bool SubscriberGotEventFromPublisherTwo { get; set; }
    }

    class PublisherOne : EndpointConfigurationBuilder
    {
        public PublisherOne() =>
            EndpointSetup<DefaultPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberPublisherOneSubscribed = true;
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    class PublisherTwo : EndpointConfigurationBuilder
    {
        public PublisherTwo() =>
            EndpointSetup<DefaultPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberPublisherTwoSubscribed = true;
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultTestServer>(_ => { }, metadata =>
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