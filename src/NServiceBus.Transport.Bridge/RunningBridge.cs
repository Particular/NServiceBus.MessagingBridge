using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport.Bridge;

class RunningBridge : IStoppableBridge
{
    public RunningBridge(EndpointProxyRegistry endpointProxyRegistry) => this.endpointProxyRegistry = endpointProxyRegistry;

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        foreach (var endpointProxy in endpointProxyRegistry.StoppableEndpoints)
        {
            await endpointProxy.Stop(cancellationToken).ConfigureAwait(false);
        }
    }

    readonly EndpointProxyRegistry endpointProxyRegistry;
}