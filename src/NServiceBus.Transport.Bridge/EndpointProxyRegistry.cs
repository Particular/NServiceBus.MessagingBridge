using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Raw;

class EndpointProxyRegistry : ITargetEndpointProxyRegistry
{
    public void RegisterProxy(
        string endpointName,
        string targetTransportName,
        IRawEndpoint proxy)
    {
        registrations.Add(new ProxyRegistration
        {
            EndpointName = endpointName,
            TranportName = targetTransportName,
            RawEndpoint = proxy
        });
    }

    public void DetermineTargetEndpointProxies(IReadOnlyCollection<BridgeTransportConfiguration> transportConfigurations)
    {
        foreach (var registration in registrations)
        {
            // target transport is the transport where this endpoint is actually running
            var targetTransportName = transportConfigurations.Single(t => t.Endpoints.Any(e => e.Name == registration.EndpointName))
                .Name;

            // just pick the first proxy that is running on the target transport since
            // we just need to be able to send messages to that transport
            var proxyEndpoint = registrations
                .First(r => r.TranportName == targetTransportName)
                .RawEndpoint;

            targetEndpointProxies[registration.EndpointName] = new TargetEndpointProxy(targetTransportName, proxyEndpoint);
        }
    }

    public TargetEndpointProxy GetTargetEndpointProxy(string sourceEndpointName)
    {
        return targetEndpointProxies[sourceEndpointName];
    }

    readonly Dictionary<string, TargetEndpointProxy> targetEndpointProxies = new Dictionary<string, TargetEndpointProxy>();
    readonly List<ProxyRegistration> registrations = new List<ProxyRegistration>();

    class ProxyRegistration
    {
        public string EndpointName;
        public string TranportName;
        public IRawEndpoint RawEndpoint;
    }
}