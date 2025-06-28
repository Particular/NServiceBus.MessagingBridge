using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Publishing_custom_address : BridgeAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                bridgeTransport.AddTestEndpoint<Publisher>();

                // setup the logical publisher to have the address of the publisher to make sure
                // that the bridge does proper address lookups when subscribing
                bridgeTransport.HasEndpoint(Conventions.EndpointNamingConvention(typeof(LogicalPublisher)), Conventions.EndpointNamingConvention(typeof(Publisher)));

                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(LogicalPublisher)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            })
            .WithEndpoint<LogicalPublisher>()
            .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, (session, _) => session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>(b => b.When(async (session, c) =>
            {
                await session.Subscribe<MyEvent>();
                if (c.HasNativePubSubSupport)
                {
                    c.SubscriberSubscribed = true;
                }
            }))
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
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(
                b => b.OnEndpointSubscribed<Context>((s, ctx) =>
                {
                    var subscriber = Conventions.EndpointNamingConvention(typeof(Subscriber));
                    if (s.SubscriberEndpoint.Contains(subscriber))
                    {
                        ctx.SubscriberSubscribed = true;
                    }
                }),
                metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    class LogicalPublisher : EndpointConfigurationBuilder
    {
        public LogicalPublisher() => EndpointSetup<DefaultServer>();
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() => EndpointSetup<DefaultTestServer>(b => b.DisableFeature<AutoSubscribe>(), metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MessageHandler(Context context) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
            {
                context.SubscriberGotEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}