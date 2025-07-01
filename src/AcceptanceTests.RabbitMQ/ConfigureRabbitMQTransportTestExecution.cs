using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureRabbitMQTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString") ?? "host=localhost";
    TestableRabbitMQTransport transport;

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        transport = new TestableRabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), connectionString);
        endpointConfiguration.UseTransport(transport);
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        PurgeQueues(transport);

        return Task.CompletedTask;
    }

    static void PurgeQueues(TestableRabbitMQTransport transport)
    {
        if (transport == null)
        {
            return;
        }

        var queues = transport.QueuesToCleanup.Distinct().ToArray();

        using var connection = ConnectionHelper.ConnectionFactory.CreateConnection("Test Queue Purger");
        using var channel = connection.CreateModel();
        foreach (var queue in queues)
        {
            try
            {
                channel.QueuePurge(queue);
                channel.QueueDelete(queue, false, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to clear queue {0}: {1}", queue, ex);
            }
        }
    }

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new TestableRabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), connectionString).ToTestableBridge();

    public Task Cleanup(BridgeTransport bridgeTransport)
    {
        PurgeQueues(bridgeTransport.FromTestableBridge<TestableRabbitMQTransport>());
        return Task.CompletedTask;
    }
}