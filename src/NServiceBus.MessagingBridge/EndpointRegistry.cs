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
        IStartableRawEndpoint startableRawEndpoint)
    {
        registrations.Add(new ProxyRegistration
        {
            Endpoint = endpoint,
            TranportName = targetTransportName,
            RawEndpoint = startableRawEndpoint
        });

        endpointAddressMappings[endpoint.Name] = endpoint.QueueAddress;
        targetEndpointAddressMappings[endpoint.QueueAddress] = startableRawEndpoint.ToTransportAddress(new QueueAddress(endpoint.Name));
    }

    public void ApplyMappings(IReadOnlyCollection<BridgeTransport> transportConfigurations)
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
        if (targetEndpointDispatchers.TryGetValue(sourceEndpointName, out var endpointDispatcher))
        {
            return endpointDispatcher;
        }

        var nearestMatch = GetNearestCaseInsensitiveMatch(sourceEndpointName, targetEndpointDispatchers.Keys);

        throw new Exception($"No target endpoint dispatcher could be found for endpoint: {sourceEndpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    public string TranslateToTargetAddress(string sourceAddress)
    {
        if (targetEndpointAddressMappings.TryGetValue(sourceAddress, out var targetAddress))
        {
            return targetAddress;
        }

        var nearestMatch = GetNearestCaseInsensitiveMatch(sourceAddress, targetEndpointAddressMappings.Keys);

        throw new Exception($"No target address mapping could be found for source address: {sourceAddress}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    public string GetEndpointAddress(string endpointName)
    {
        if (endpointAddressMappings.TryGetValue(endpointName, out var address))
        {
            return address;
        }

        var nearestMatch = GetNearestCaseInsensitiveMatch(endpointName, endpointAddressMappings.Keys);

        throw new Exception($"No address mapping could be found for endpoint: {endpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    static string GetNearestCaseInsensitiveMatch(string sourceEndpointName, IEnumerable<string> items)
    {
        var results = new List<(int distance, string value)>();
        var calculator = new Levenshtein(sourceEndpointName.ToLower());
        var nearestMatch = items
            .OrderBy(x => calculator.DistanceFrom(x.ToLower()))
            .First();
        return nearestMatch;
    }

    public IEnumerable<ProxyRegistration> Registrations => registrations;

    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointDispatchers = new Dictionary<string, TargetEndpointDispatcher>();
    readonly Dictionary<string, string> targetEndpointAddressMappings = new Dictionary<string, string>();
    readonly Dictionary<string, string> endpointAddressMappings = new Dictionary<string, string>();
    readonly List<ProxyRegistration> registrations = new List<ProxyRegistration>();

    public class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public string TranportName;
        public IStartableRawEndpoint RawEndpoint;
    }
}