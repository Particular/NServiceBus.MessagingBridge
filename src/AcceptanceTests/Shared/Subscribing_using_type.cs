using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Subscribing_using_type : RouterAcceptanceTest
{
    [Test]
    public async Task Should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Subscriber>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>().ConfigureAwait(false);

                // The test transport have native pubsub so we can set the flag here
                ctx.SubscriberSubscribed = true;
            }))
            .WithEndpoint<Publisher>(b => b
                .When(c => c.SubscriberSubscribed, (session, c) =>
                {
                    var options = new PublishOptions();

                    return session.Publish(new MyEvent(), options);
                }))
            .WithRouter(routerConfiguration =>
            {
                routerConfiguration.AddTransport(TransportBeingTested)
                    .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)))
                    .RegisterPublisher(typeof(MyEvent), Conventions.EndpointNamingConvention(typeof(Publisher)));

                AddTestTransport(routerConfiguration)
                    .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Publisher)));
            })
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
            EndpointSetup<DefaultTestPublisher>();
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<AutoSubscribe>();
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