using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Publishing : RouterAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithRouter(routerConfiguration =>
            {
                routerConfiguration.AddTransport(TransportBeingTested)
                    .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Publisher)));

                AddTestTransport(routerConfiguration)
                    .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)))
                     .RegisterPublisher(typeof(MyEvent).FullName, Conventions.EndpointNamingConvention(typeof(Publisher)));
            })
            .WithEndpoint<Publisher>(b => b
                .When(c => TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, c) =>
                {
                    return session.Publish(new MyEvent());
                }))
            .WithEndpoint<Subscriber>()
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
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
            });
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultTestServer>(publisherMetadata: p => p.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
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