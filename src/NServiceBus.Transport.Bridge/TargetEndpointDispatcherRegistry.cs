using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Raw;

class TargetEndpointDispatcherRegistry : ITargetEndpointDispatcherRegistry
{
    public void RegisterDispatcher(
        BridgeEndpoint endpoint,
        string targetTransportName,
        IRawEndpoint proxy)
    {
        registrations.Add(new ProxyRegistration
        {
            Endpoint = endpoint,
            TranportName = targetTransportName,
            RawEndpoint = proxy
        });
    }

    public void DetermineTargetEndpointDispatchers(IReadOnlyCollection<BridgeTransportConfiguration> transportConfigurations)
    {
        foreach (var registration in registrations)
        {
            // target transport is the transport where this endpoint is actually running
            var targetTransportName = transportConfigurations.Single(t => t.Endpoints.Any(e => e.Name == registration.Endpoint.Name))
                .Name;

            // just pick the first proxy that is running on the target transport since
            // we just need to be able to send messages to that transport
            var proxyEndpoint = registrations
                .First(r => r.TranportName == targetTransportName)
                .RawEndpoint;

            targetEndpointProxies[registration.Endpoint.Name] = new TargetEndpointDispatcher(
                targetTransportName,
                proxyEndpoint,
                registration.Endpoint.QueueAddress);
        }
    }

    public TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName)
    {
        return targetEndpointProxies[sourceEndpointName];
    }

    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointProxies = new Dictionary<string, TargetEndpointDispatcher>();
    readonly List<ProxyRegistration> registrations = new List<ProxyRegistration>();

    class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public string TranportName;
        public IRawEndpoint RawEndpoint;
    }
}