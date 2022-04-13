using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

public class BridgeTransportDefinition
{
    public BridgeTransportConfiguration TransportConfiguration { get; set; }

    public Func<CancellationToken, Task> Cleanup { get; set; }
}