namespace AcceptanceTests.SqlServer.MultiSchema
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Transport.SqlServer;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_custom_schema_configured_for_endpoint : BridgeAcceptanceTest
    {
        public const string PublisherSchema = "publisher";

        [Test]
        public async Task Subscriber_should_get_the_event()
        {
            var context = await Scenario.Define<Context>()
            .WithBridge(bridgeConfiguration =>
            {
                var endpointAddress = GetTestEndpointAddress<Publisher>();
                var bridgeTransport = new BridgeTransport(TransportBeingTested);
                var endpointName = Conventions.EndpointNamingConvention(typeof(Publisher));
                bridgeTransport.AddTestEndpoint<Publisher>();
                //bridgeTransport.HasEndpoint(endpointName, "WhenCustomSchemaConfiguredForEndpoint.Publisher@[publisher]@[NServiceBus]");
                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            })
            .WithEndpoint<Publisher>(b => b
                .When(c => TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, c) =>
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
                    //var endpointName = Conventions.EndpointNamingConvention(typeof(Publisher));
                    var transport = c.ConfigureSqlServerTransport();
                    transport.DefaultSchema = PublisherSchema;
                    transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName("SubscriptionRouting", "dbo");
                    transport.Subscriptions.DisableCaching = true;

                    //transport.SchemaAndCatalog.UseSchemaForQueue(endpointName, PublisherSchema);

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
                EndpointSetup<DefaultTestServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                });
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
}

