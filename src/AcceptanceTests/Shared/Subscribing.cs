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
            .WithEndpoint<Subscriber>()
            .WithEndpoint<Publisher>(b => b
                .When((session, _) =>
                {
                    return session.Publish(new MyEvent());
                }))
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));

                bridgeTransport.HasEndpoint(subscriberEndpoint);

                bridgeConfiguration.AddTransport(bridgeTransport);

                bridgeConfiguration.AddTestTransportEndpoint<Publisher>();
            })
            .Done(c => c.SubscriberGotEvent)
            .Run();

        Assert.That(context.SubscriberGotEvent, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool SubscriberGotEvent { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultTestPublisher>(_ => { }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(_ => { }, metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());
        }

        public class MessageHandler : IHandleMessages<MyEvent>
        {
            Context context;

            public MessageHandler(Context context) => this.context = context;

            public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
            {
                context.SubscriberGotEvent = true;
                return Task.FromResult(0);
            }
        }
    }

    public class MyEvent : IEvent
    {
    }
}