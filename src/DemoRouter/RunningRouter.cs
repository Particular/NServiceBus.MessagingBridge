using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;

public class RunningRouter
{
    readonly List<IReceivingRawEndpoint> stoppableRawEndpoints;

    public RunningRouter(List<IReceivingRawEndpoint> stoppableRawEndpoints)
    {
        this.stoppableRawEndpoints = stoppableRawEndpoints;
    }
    public async Task Stop(CancellationToken cancellationToken = default)
    {
        foreach (var stoppableRawEndpoint in stoppableRawEndpoints)
        {
            await stoppableRawEndpoint.Stop(cancellationToken).ConfigureAwait(false);
        }
    }
}