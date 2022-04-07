using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureAzureServiceBusTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string? connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
    public BridgeTransportDefinition GetBridgeTransport()
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No connectionstring for found in environment variable 'AzureServiceBus_ConnectionString'");
        }

        var transportDefinition = new TestableAzureServiceBusTransport(connectionString)
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        };

        return new BridgeTransportDefinition()
        {
            TransportDefinition = transportDefinition,
            Cleanup = (ct) => Cleanup(transportDefinition, ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableAzureServiceBusTransport(connectionString);
        endpointConfiguration.UseTransport(transportDefinition);

        return ct => Cleanup(transportDefinition, ct);
    }

    Task Cleanup(TestableAzureServiceBusTransport transport, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            transport.EnablePartitioning = true;
        }

        return Task.CompletedTask;
    }
}