using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Separate_instances : BridgeAcceptanceTest
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
            .WithBridge((bridgeConfiguration, _) =>
            {
                // Publisher SQL Transport
                var publisherBridgeTransport = new TestableSqlServerTransport(connectionString).ToTestableBridge("PublisherTransport");
                // Add endpoints
                publisherBridgeTransport.AddTestEndpoint<Publisher>();

                // Subscriber Sql Transport
                var subscriberBridgeTransport = new TestableSqlServerTransport(connectionString2).ToTestableBridge("SubscriberTransport");
                // Add endpoints
                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));
                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                subscriberBridgeTransport.HasEndpoint(subscriberEndpoint);

                bridgeConfiguration.AddTransport(publisherBridgeTransport);
                bridgeConfiguration.AddTransport(subscriberBridgeTransport);

                // DTC won't work
                bridgeConfiguration.RunInReceiveOnlyTransactionMode();
            })
            .WithEndpoint<Publisher>(b => b.When(c => c.EndpointsStarted, (session, _) => session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotEvent)
            .Run();

        Assert.That(context.SubscriberGotEvent, Is.True);
    }

    public class Context : BridgeScenarioContext
    {
        public bool SubscriberGotEvent { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(b =>
            {
                var transport = new SqlServerTransport(connectionString);
                b.UseTransport(transport);
            });
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>(b =>
            {
                var transport = new SqlServerTransport(connectionString2);
                b.UseTransport(transport);
            });

        public class MessageHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
            {
                testContext.SubscriberGotEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}