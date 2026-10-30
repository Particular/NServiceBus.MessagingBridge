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

    public Task Cleanup() => PurgeQueues(transport);

    static async Task PurgeQueues(TestableRabbitMQTransport transport)
    {
        if (transport == null)
        {
            return;
        }

        // The shared error queue is used by all tests running in parallel
        var queues = transport.QueuesToCleanup.Distinct().Where(queue => queue != "error").ToArray();

        await using var connection = await ConnectionHelper.ConnectionFactory.CreateConnectionAsync("Test Queue Purger");
        await using var channel = await connection.CreateChannelAsync();
        foreach (var queue in queues)
        {
            try
            {
                await channel.QueuePurgeAsync(queue);
                await channel.QueueDeleteAsync(queue, false, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to clear queue {0}: {1}", queue, ex);
            }
        }
    }

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new TestableRabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), connectionString).ToTestableBridge();

    public Task Cleanup(BridgeTransport bridgeTransport) => PurgeQueues(bridgeTransport.FromTestableBridge<TestableRabbitMQTransport>());
}