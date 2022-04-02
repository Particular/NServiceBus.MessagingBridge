using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

public class RouterTransportDefinition
{
    public TransportDefinition TransportDefinition { get; set; }

    public Func<CancellationToken, Task> Cleanup { get; set; }
}