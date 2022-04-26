namespace AcceptanceTests.SqlServer.MultiSchema
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Transport.SqlServer;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_custom_schema_configured_for_both_endpoints : BridgeAcceptanceTest
    {
        const string PublisherSchema = "publisher";
        const string SubscriberSchema = "subscriber";

        //public const string SubscriberSchema = "dbo";
        readonly string connectionString = Environment.GetEnvironmentVariable("SqlServerTransportConnectionString");

        [Test]
        public async Task Subscriber_should_get_the_event()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b
                    .When(c => TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, c) =>
                    {
                        return session.Publish(new MyEvent());
                    }))
                .WithEndpoint<Subscriber>()
                .WithBridge(bridgeConfiguration =>
                {
                    // Publisher SQL Transport
                    var publisherSqlTransport = new TestableSqlServerTransport(connectionString)
                    {
                        DefaultSchema = PublisherSchema
                    };
                    // Publisher Bridge Transport
                    var publisherBridgeTransport = new BridgeTransport(publisherSqlTransport)
                    {
                        Name = "publisherBridgeTransport"
                    };
                    // Add endpoint to bridge transport
                    publisherBridgeTransport.AddTestEndpoint<Publisher>();

                    // Subscriber Sql Transport
                    var subscriberSqlTransport = new TestableSqlServerTransport(connectionString)
                    {
                        DefaultSchema = SubscriberSchema
                    };
                    // Subscriber Bridge Transport
                    var subscriberBridgeTransport = new BridgeTransport(subscriberSqlTransport)
                    {
                        Name = "subscriberBridgeTransport"
                    };
                    // Add endpoint to bridge transport
                    var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));
                    subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                    subscriberBridgeTransport.HasEndpoint(subscriberEndpoint);

                    // Add bridge transports to the bridge configuration
                    bridgeConfiguration.AddTransport(publisherBridgeTransport);
                    bridgeConfiguration.AddTransport(subscriberBridgeTransport);
                    //bridgeConfiguration.RunInReceiveOnlyTransactionMode();
                })
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
                    var transport = c.ConfigureSqlServerTransport();
                    transport.DefaultSchema = PublisherSchema;
                    transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName("SubscriptionRouting", SubscriberSchema);
                    // transport.Subscriptions.DisableCaching = true;

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
                EndpointSetup<DefaultServer>(c =>
                {
                    var transport = c.ConfigureSqlServerTransport();
                    transport.DefaultSchema = SubscriberSchema;
                    transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName("SubscriptionRouting", SubscriberSchema);
                    // transport.Subscriptions.SubscriptionTableName =
                    //     new SubscriptionTableName("SubscriptionRouting", "dbo");
                    // transport.Subscriptions.DisableCaching = true;

                    // c.DisableFeature<AutoSubscribe>();
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