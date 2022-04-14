using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

public class BridgeTransportDefinition
{
    public BridgeTransportDefinition()
    {
        GetEndpointAddress = (endpointName) => endpointName;
    }

    public TransportDefinition TransportDefinition { get; set; }

    public Func<string, string> GetEndpointAddress { get; set; }

    public Func<CancellationToken, Task> Cleanup { get; set; }
}