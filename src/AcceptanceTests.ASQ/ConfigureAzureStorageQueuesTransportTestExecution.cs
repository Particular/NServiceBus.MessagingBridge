using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureAzureStorageQueuesTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("AzureStorageQueueTransport_ConnectionString");
    public BridgeTransportDefinition GetBridgeTransport()
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No connectionstring for found in environment variable 'AzureStorageQueueTransport_ConnectionString'");
        }

        var transportDefinition = new TestableAzureStorageQueuesTransport(connectionString);

        return new BridgeTransportDefinition()
        {
            TransportDefinition = transportDefinition,
            Cleanup = (ct) => Cleanup(ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableAzureStorageQueuesTransport(connectionString);

        endpointConfiguration.UseTransport(transportDefinition);

        return ct => Cleanup(ct);
    }

    Task Cleanup(CancellationToken cancellationToken) => Task.CompletedTask;
}
