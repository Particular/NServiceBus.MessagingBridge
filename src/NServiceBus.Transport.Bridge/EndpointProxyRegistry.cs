using System.Collections.Generic;
using System.Linq;
using NServiceBus.Raw;

class EndpointProxyRegistry
{
    public void AddProxy(string name, IStoppableRawEndpoint stoppableRawEndpoint)
    {
        runningEndpoints.Add((name, stoppableRawEndpoint));
    }

    public IEnumerable<IStoppableRawEndpoint> StoppableEndpoints => runningEndpoints.Select(e => e.Item2);

    public IEnumerable<IRawEndpoint> GetTargetEndpointProxies(string endpointName)
    {
        //TODO: do we need to cache for better perf?
        return runningEndpoints.Where(e => e.Item1 != endpointName)
            .Select(i => i.Item2 as IRawEndpoint);
    }

    readonly List<(string, IStoppableRawEndpoint)> runningEndpoints = new List<(string, IStoppableRawEndpoint)>();
}