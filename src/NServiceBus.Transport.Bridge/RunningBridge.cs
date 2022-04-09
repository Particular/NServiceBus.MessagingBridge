using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Transport.Bridge;

class RunningBridge : IStoppableBridge
{
    public RunningBridge(List<IStoppableRawEndpoint> stoppableRawEndpoints)
        => this.stoppableRawEndpoints = stoppableRawEndpoints;

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        foreach (var endpointProxy in stoppableRawEndpoints)
        {
            await endpointProxy.Stop(cancellationToken).ConfigureAwait(false);
        }
    }

    readonly List<IStoppableRawEndpoint> stoppableRawEndpoints;
}