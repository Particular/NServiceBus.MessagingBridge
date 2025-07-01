using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class ConfigureAzureStorageQueuesTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("AzureStorageQueueTransport_ConnectionString");

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        Assert.That(connectionString, Is.Not.Null, "AzureStorageQueueTransport_ConnectionString environment variable must be set for Azure Storage Queues acceptance tests.");

        var transportDefinition = new TestableAzureStorageQueuesTransport(connectionString);

        endpointConfiguration.UseTransport(transportDefinition);
        return Task.CompletedTask;
    }

    public Task Cleanup() => Task.CompletedTask;

    public BridgeTransport Configure(PublisherMetadata publisherMetadata)
    {
        Assert.That(connectionString, Is.Not.Null, "AzureStorageQueueTransport_ConnectionString environment variable must be set for Azure Storage Queues acceptance tests.");

        return new TestableAzureStorageQueuesTransport(connectionString).ToTestableBridge();
    }

    public Task Cleanup(BridgeTransport bridgeTransport) => Task.CompletedTask;
}
