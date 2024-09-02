using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;

class EndpointRegistry(EndpointProxyFactory endpointProxyFactory, ILogger<StartableBridge> logger) : IEndpointRegistry
{
    public async Task ApplyMappings(IReadOnlyCollection<BridgeTransport> transportConfigurations, CancellationToken cancellationToken = default)
    {
        // create required proxy endpoints on all transports
        foreach (var transportConfiguration in transportConfigurations)
        {
            logger.LogInformation("Creating proxies for transport {name}", transportConfiguration.Name);

            // get all endpoints that we need to proxy in this transport, ie all that don't exist this transport.
            var endpoints = transportConfigurations.Where(s => s != transportConfiguration).SelectMany(s => s.Endpoints);

            // create the proxy and subscribe it to configured events
            foreach (var endpointToSimulate in endpoints)
            {
                var startableEndpointProxy = await endpointProxyFactory.CreateProxy(
                   endpointToSimulate,
                   transportConfiguration,
                   cancellationToken)
                .ConfigureAwait(false);

                logger.LogInformation("Proxy for endpoint {endpoint} created on {transport}", endpointToSimulate.Name, transportConfiguration.Name);

                registrations.Add(new ProxyRegistration
                {
                    Endpoint = endpointToSimulate,
                    TranportName = transportConfiguration.Name,
                    RawEndpoint = startableEndpointProxy
                });
            }
        }

        foreach (var registration in registrations)
        {
            var endpoint = registration.Endpoint;

            // target transport is the transport where this endpoint is actually running
            var targetTransport = transportConfigurations.Single(t => t.Endpoints.Any(e => e.Name == endpoint.Name));

            // just pick the first proxy that is running on the target transport since
            // we just need to be able to send messages to that transport
            var proxyEndpoint = registrations
                .First(r => r.TranportName == targetTransport.Name)
                .RawEndpoint;

            //This value represents in fact the endpoint's name. It is wrapped in the QueueAddress class only because
            //the ToTransportAddress API expects it.
            var endpointName = new QueueAddress(endpoint.Name);

            var transportAddress = endpoint.QueueAddress ?? proxyEndpoint.ToTransportAddress(endpointName);

            endpointAddressMappings[registration.Endpoint.Name] = transportAddress;

            targetEndpointAddressMappings[transportAddress] = registration.RawEndpoint.ToTransportAddress(endpointName);

            targetEndpointDispatchers[registration.Endpoint.Name] = new TargetEndpointDispatcher(
                targetTransport.Name,
                proxyEndpoint,
                transportAddress);
        }
    }

    public TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName)
    {
        if (targetEndpointDispatchers.TryGetValue(sourceEndpointName, out var endpointDispatcher))
        {
            return endpointDispatcher;
        }

        var nearestMatch = GetClosestMatchForExceptionMessage(sourceEndpointName, targetEndpointDispatchers.Keys);

        throw new Exception($"No target endpoint dispatcher could be found for endpoint: {sourceEndpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    public bool TryTranslateToTargetAddress(string sourceAddress, out string bestMatch)
    {
        if (targetEndpointAddressMappings.TryGetValue(sourceAddress, out bestMatch))
        {
            return true;
        }

        bestMatch = GetClosestMatchForExceptionMessage(sourceAddress, targetEndpointAddressMappings.Keys);
        return false;
    }

    public string GetEndpointAddress(string endpointName)
    {
        if (endpointAddressMappings.TryGetValue(endpointName, out var address))
        {
            return address;
        }

        var nearestMatch = GetClosestMatchForExceptionMessage(endpointName, endpointAddressMappings.Keys);

        throw new Exception($"No address mapping could be found for endpoint: {endpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    static string GetClosestMatchForExceptionMessage(string sourceEndpointName, IEnumerable<string> items)
    {
        var calculator = new Levenshtein(sourceEndpointName.ToLower());
        var nearestMatch = items
            .OrderBy(x => calculator.DistanceFrom(x.ToLower()))
            .FirstOrDefault();
        return nearestMatch ?? "(No mappings registered)";
    }

    public IEnumerable<ProxyRegistration> Registrations => registrations;

    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointDispatchers = [];
    readonly Dictionary<string, string> targetEndpointAddressMappings = [];
    readonly Dictionary<string, string> endpointAddressMappings = [];
    readonly List<ProxyRegistration> registrations = [];

    public class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public string TranportName;
        public IStartableRawEndpoint RawEndpoint;
    }
}