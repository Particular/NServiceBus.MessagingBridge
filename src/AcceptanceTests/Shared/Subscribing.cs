using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Subscribing : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<SubscribedEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));

                transportBeingTested.HasEndpoint(subscriberEndpoint);

                bridgeConfiguration.AddTransport(transportBeingTested);

                bridgeConfiguration.AddTestTransportEndpoint<Publisher>();
            }, metadata => metadata.RegisterPublisherFor<SubscribedEvent, Publisher>())
            .WithEndpoint<Subscriber>()
            .WithEndpoint<Publisher>(b => b.When((session, _) => session.Publish(new SubscribedEvent())))
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
        public Publisher() => EndpointSetup<DefaultTestPublisher>(_ => { }, metadata => metadata.RegisterSelfAsPublisherFor<SubscribedEvent>(this));
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() => EndpointSetup<DefaultServer>(_ => { }, metadata => metadata.RegisterPublisherFor<SubscribedEvent, Publisher>());

        public class MessageHandler(Context context) : IHandleMessages<SubscribedEvent>
        {
            public Task Handle(SubscribedEvent message, IMessageHandlerContext handlerContext)
            {
                context.SubscriberGotEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    // Event names must be unique across fixtures: the RabbitMQ conventional topology derives exchange names from the type name only.
    public class SubscribedEvent : IEvent;
}