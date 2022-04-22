namespace AcceptanceTests.SqlServer.MultiSchema
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Transport.SqlServer;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_custom_schema_configured_for_one_endpoint : BridgeAcceptanceTest
    {
        public const string PublisherSchema = "publisher";
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
                var testableSqlServerTransport = new TestableSqlServerTransport(connectionString);
                var publisherBridgeTransport = new BridgeTransport(testableSqlServerTransport)
                {
                    Name = "publisherBridgeTransport"
                };
                publisherBridgeTransport.AddTestEndpoint<Publisher>();
                bridgeConfiguration.AddTransport(publisherBridgeTransport);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));
                var subscriberBridgeTransport = new BridgeTransport(testableSqlServerTransport)
                {
                    Name = "subscriberBridgeTransport"
                };
                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                subscriberBridgeTransport.HasEndpoint(subscriberEndpoint);

                bridgeConfiguration.AddTransport(subscriberBridgeTransport);
                //bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
                //bridgeConfiguration.AddTestTransportEndpoint<Subscriber>();
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
                    transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName("SubscriptionRouting", "dbo");
                    transport.Subscriptions.DisableCaching = true;

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
                    //transport.DefaultSchema = SubscriberSchema;
                    transport.Subscriptions.SubscriptionTableName = new SubscriptionTableName("SubscriptionRouting", "dbo");
                    transport.Subscriptions.DisableCaching = true;

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

