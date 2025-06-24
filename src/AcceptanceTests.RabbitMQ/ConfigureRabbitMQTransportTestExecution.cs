using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureRabbitMQTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString") ?? "host=localhost";
    TestableRabbitMQTransport transport;

    public BridgeTransportDefinition GetBridgeTransport() =>
        new()
        {
            TransportDefinition = new TestableRabbitMQTransport(
                RoutingTopology.Conventional(QueueType.Quorum),
                connectionString),
            Cleanup = (ct) => Cleanup(ct)
        };

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        transport = new TestableRabbitMQTransport(
            RoutingTopology.Conventional(QueueType.Quorum),
            connectionString);
        endpointConfiguration.UseTransport(transport);

        return Cleanup;
    }

    async Task Cleanup(CancellationToken cancellationToken) => await PurgeQueues();

    async Task PurgeQueues()
    {
        if (transport == null)
        {
            return;
        }

        var queues = transport.QueuesToCleanup.Distinct().ToArray();

        await using var connection = await ConnectionHelper.ConnectionFactory.CreateConnectionAsync("Test Queue Purger");
        await using var channel = await connection.CreateChannelAsync();

        foreach (var queue in queues)
        {
            try
            {
                _ = await channel.QueuePurgeAsync(queue);
                _ = await channel.QueueDeleteAsync(queue, false, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to clear queue {0}: {1}", queue, ex);
            }
        }
    }

}