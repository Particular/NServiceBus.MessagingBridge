using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;

class EndpointRegistry : IEndpointRegistry
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

        addressMappings[endpoint.QueueAddress] = proxy.ToTransportAddress(new QueueAddress(endpoint.Name));
    }

    public void ApplyMappings(IReadOnlyCollection<BridgeTransportConfiguration> transportConfigurations)
    {
        foreach (var registration in registrations)
        {
            // target transport is the transport where this endpoint is actually running
            var targetTransport = transportConfigurations.Single(t => t.Endpoints.Any(e => e.Name == registration.Endpoint.Name));

            // just pick the first proxy that is running on the target transport since
            // we just need to be able to send messages to that transport
            var proxyEndpoint = registrations
                .First(r => r.TranportName == targetTransport.Name)
                .RawEndpoint;

            targetEndpointDispatchers[registration.Endpoint.Name] = new TargetEndpointDispatcher(
                targetTransport.Name,
                proxyEndpoint,
                registration.Endpoint.QueueAddress);
        }
    }

    public TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName)
    {
        return targetEndpointDispatchers[sourceEndpointName];
    }

    public string TranslateToTargetAddress(string sourceAddress)
    {
        if (addressMappings.TryGetValue(sourceAddress, out var targetAddress))
        {
            return targetAddress;
        }

        throw new Exception($"No address mapping could be found for {sourceAddress}");
    }

    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointDispatchers = new Dictionary<string, TargetEndpointDispatcher>();
    readonly Dictionary<string, string> addressMappings = new Dictionary<string, string>();
    readonly List<ProxyRegistration> registrations = new List<ProxyRegistration>();

    class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public string TranportName;
        public IRawEndpoint RawEndpoint;
    }
}