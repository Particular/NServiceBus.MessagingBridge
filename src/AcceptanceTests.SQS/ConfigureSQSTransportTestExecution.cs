using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureSQSTransportTestExecution : IConfigureTransportTestExecution
{
    public BridgeTransportDefinition GetBridgeTransport()
    {
        //var transportDefinition = new TestableSQSTransport()
        //{
        //    TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        //};

        var transportDefinition = new TestableSQSTransport();

        return new BridgeTransportDefinition()
        {
            TransportDefinition = transportDefinition,
            Cleanup = (ct) => Cleanup(transportDefinition, ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableSQSTransport();
        endpointConfiguration.UseTransport(transportDefinition);

        return ct => Cleanup(transportDefinition, ct);
    }

    Task Cleanup(TestableSQSTransport transport, CancellationToken cancellationToken)
    {
        _ = transport;
        //if (cancellationToken.IsCancellationRequested)
        //{
        //    transport.EnablePartitioning = true;
        //}

        return Task.CompletedTask;
    }
}