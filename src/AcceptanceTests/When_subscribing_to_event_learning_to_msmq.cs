using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class When_subscribing_to_event_learning_to_msmq
{
    static LearningTransport publisherTransport;
    static MsmqTransport subscriberTransport;

    [Test]
    public async Task Should_get_the_event()
    {
        var routerConfiguration = new MessageRouterConfiguration();

        subscriberTransport = new MsmqTransport();
        publisherTransport = new LearningTransport();

        routerConfiguration.AddTransport(publisherTransport)
            .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Publisher)));

        var publisherEndpoint = Conventions.EndpointNamingConvention(typeof(Publisher));
        routerConfiguration.AddTransport(subscriberTransport)
            .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)))
            .RegisterPublisher(typeof(MyEvent), Conventions.EndpointNamingConvention(typeof(Publisher)));

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(c => c.SubscriberSubscribed, (session, c) =>
                {
                    c.AddTrace("Subscriber is subscribed, going to publish MyEvent");

                    var options = new PublishOptions();

                    return session.Publish(new MyEvent(), options);
                }))
            .WithEndpoint<Subscriber>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>().ConfigureAwait(false);
                if (ctx.HasNativePubSubSupport)
                {
                    ctx.SubscriberSubscribed = true;
                    ctx.AddTrace("Subscriber is now subscribed (at least we have asked the broker to be subscribed");
                }
                else
                {
                    ctx.SubscriberSubscribed = true;
                    ctx.AddTrace("Subscriber has now asked to be subscribed to MyEvent");
                }
            }))
            .WithRouter(routerConfiguration)
            .Done(c => c.SubscriberGotEvent)
            .Run().ConfigureAwait(false);

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
                c.UseTransport(publisherTransport);
                //c.ConfigureRouting().RouteToEndpoint(typeof(MyEvent), typeof(Subscriber));
                // c.UsePersistence<MsmqPersistence, StorageType.Subscriptions>();
                c.OnEndpointSubscribed<Context>((s, context) =>
                {
                    var subscriber = Conventions.EndpointNamingConvention(typeof(Subscriber));
                    if (s.SubscriberEndpoint.Contains(subscriber))
                    {
                        context.SubscriberSubscribed = true;
                        context.AddTrace($"{subscriber} is now subscribed");
                    }
                });
                // c.DisableFeature<AutoSubscribe>();
            });
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                // c.DisableFeature<AutoSubscribe>();
                c.UseTransport(new MsmqTransport());
                c.UsePersistence<MsmqPersistence, StorageType.Subscriptions>();
            }, p => p.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
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