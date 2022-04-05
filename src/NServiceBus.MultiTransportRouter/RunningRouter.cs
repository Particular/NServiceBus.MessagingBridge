using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;

public class RunningRouter
{
    public RunningRouter(List<IReceivingRawEndpoint> runningEndpoints) => this.runningEndpoints = runningEndpoints;

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        foreach (var endpoint in runningEndpoints)
        {
            await endpoint.Stop(cancellationToken).ConfigureAwait(false);
        }
    }

    readonly List<IReceivingRawEndpoint> runningEndpoints;
}