using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Publishing : BridgeAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                transportBeingTested.AddTestEndpoint<Publisher>();
                bridgeConfiguration.AddTransport(transportBeingTested);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<PublishedEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            }, metadata => metadata.RegisterPublisherFor<PublishedEvent, Publisher>())
            .WithEndpoint<Publisher>(b => b
                .When(c => c.TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, _) => session.Publish(new PublishedEvent())))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotEvent)
            .Run();

        Assert.That(context.SubscriberGotEvent, Is.True);
    }

    public class Context : BridgeScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool SubscriberGotEvent { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<PublishedEvent>(this));
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() => EndpointSetup<DefaultTestServer>(_ => { }, metadata => metadata.RegisterPublisherFor<PublishedEvent, Publisher>());

        public class MessageHandler(Context context) : IHandleMessages<PublishedEvent>
        {
            public Task Handle(PublishedEvent message, IMessageHandlerContext handlerContext)
            {
                context.SubscriberGotEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    // Event names must be unique across fixtures: the RabbitMQ conventional topology derives exchange names from the type name only.
    public class PublishedEvent : IEvent;
}