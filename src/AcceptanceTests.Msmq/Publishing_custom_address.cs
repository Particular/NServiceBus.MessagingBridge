using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
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
            .WithEndpoint<Publisher>(b => b
                .When(c => c.SubscriberSubscribed, (session, c) =>
                {
                    return session.Publish(new MyEvent());
                }))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotEvent)
            .Run();

        Assert.IsTrue(context.SubscriberGotEvent);
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
            EndpointSetup<DefaultPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
            });
        }
    }

    class LogicalPublisher : EndpointConfigurationBuilder
    {
        public LogicalPublisher()
        {
            EndpointSetup<DefaultServer>();
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultTestServer>();
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