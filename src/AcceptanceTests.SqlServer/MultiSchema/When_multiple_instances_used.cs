namespace AcceptanceTests.SqlServer.MultiSchema
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_multiple_instances_used : BridgeAcceptanceTest
    {
        static readonly string connectionString = Environment.GetEnvironmentVariable("SqlServerTransportConnectionString");
        static string connectionString2 = string.Empty;

        [Test]
        public async Task Subscriber_should_get_the_event()
        {
            if (connectionString.IndexOf("nservicebus", StringComparison.OrdinalIgnoreCase) == -1)
            {
                throw new InvalidOperationException(
                    "Can't find database 'nservicebus' in connectionstring. Can't run the test this way.");
            }

            connectionString2 = connectionString.Replace("nservicebus", "nservicebus2");

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
                    var publisherSqlTransport = new TestableSqlServerTransport(connectionString);
                    var publisherBridgeTransport = new BridgeTransport(publisherSqlTransport)
                    {
                        Name = "publisherBridgeTransport"
                    };
                    // Add endpoints
                    publisherBridgeTransport.AddTestEndpoint<Publisher>();

                    // Subscriber Sql Transport
                    var subscriberSqlTransport = new TestableSqlServerTransport(connectionString2);
                    var subscriberBridgeTransport = new BridgeTransport(subscriberSqlTransport)
                    {
                        Name = "subscriberBridgeTransport"
                    };
                    // Add endpoints
                    var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));
                    subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                    subscriberBridgeTransport.HasEndpoint(subscriberEndpoint);

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
                    var transport = new SqlServerTransport(connectionString);

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
                    var transport = new SqlServerTransport(connectionString2);
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