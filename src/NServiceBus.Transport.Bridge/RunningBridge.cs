using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport.Bridge;

class RunningBridge : IStoppableBridge
{
    public RunningBridge(List<EndpointProxy> endpointProxies) => this.endpointProxies = endpointProxies;

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        foreach (var endpoint in endpointProxies)
        {
            await endpoint.Stop(cancellationToken).ConfigureAwait(false);
        }
    }

    readonly List<EndpointProxy> endpointProxies;
}