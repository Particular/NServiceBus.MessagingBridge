using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport.RabbitMQ;

class ConfigureRabbitMQTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString") ?? "host=localhost";
    TestableRabbitMQTransport transport;

    public BridgeTransportDefinition GetBridgeTransport()
    {
        var transportDefinition = new TestableRabbitMQTransport(
                new ConventionalRoutingTopology(true),
                connectionString);

        return new BridgeTransportDefinition
        {
            TransportConfiguration = new BridgeTransportConfiguration(transportDefinition),
            Cleanup = (ct) => Cleanup(ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        transport = new TestableRabbitMQTransport(
            new ConventionalRoutingTopology(true),
            connectionString);
        endpointConfiguration.UseTransport(transport);

        return Cleanup;
    }

    Task Cleanup(CancellationToken cancellationToken)
    {
        PurgeQueues();

        return Task.CompletedTask;
    }

    void PurgeQueues()
    {
        if (transport == null)
        {
            return;
        }

        var queues = transport.QueuesToCleanup.Distinct().ToArray();

        using (var connection = ConnectionHelper.ConnectionFactory.CreateConnection("Test Queue Purger"))
        using (var channel = connection.CreateModel())
        {
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
    }

}