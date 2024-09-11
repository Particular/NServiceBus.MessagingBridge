using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;

class EndpointRegistry(EndpointProxyFactory endpointProxyFactory, ILogger<StartableBridge> logger) : IEndpointRegistry
{
    public async IAsyncEnumerable<ProxyRegistration> Initialize(IReadOnlyCollection<BridgeTransport> transportConfigurations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Assume that it is the number of endpoints that is more likely to scale up in size than the number of transports (typically only 2).
        // Therefore in cases where it might matter, it is more efficient to iterate over the transports multiple times.
        transportConfigurationMappings = transportConfigurations.ToDictionary(t => t.Name, t => t);

        // create required proxy endpoints on all transports
        foreach (var targetTransport in transportConfigurations)
        {
            var dispatcher = await DispatcherFactory.CreateDispatcher(targetTransport, cancellationToken).ConfigureAwait(false);

            foreach (var endpointToSimulate in targetTransport.Endpoints)
            {
                // Endpoint will need to be proxied on the other transports
                foreach (var proxyTransport in transportConfigurationMappings.Where(kvp => kvp.Key != targetTransport.Name).Select(kvp => kvp.Value))
                {
                    var startableEndpointProxy = await endpointProxyFactory.CreateProxy(endpointToSimulate, proxyTransport, cancellationToken)
                        .ConfigureAwait(false);

                    logger.LogInformation("Proxy for endpoint {endpoint} created on {transport}", endpointToSimulate.Name, proxyTransport.Name);

                    var queueAddress = new QueueAddress(endpointToSimulate.Name);
                    var targetTransportAddress = dispatcher.ToTransportAddress(queueAddress);
                    var sourceTransportAddress = startableEndpointProxy.ToTransportAddress(queueAddress);

                    endpointAddressMappings[endpointToSimulate.Name] = endpointToSimulate.QueueAddress ?? targetTransportAddress;
                    targetEndpointAddressMappings[targetTransportAddress] = sourceTransportAddress;
                    if (targetTransportAddress != sourceTransportAddress)
                    {
                        targetEndpointAddressMappings[sourceTransportAddress] = targetTransportAddress;
                    }
                    targetEndpointDispatchers[endpointToSimulate.Name] = new TargetEndpointDispatcher(targetTransport.Name, dispatcher, targetTransportAddress);

                    yield return new ProxyRegistration
                    {
                        Endpoint = endpointToSimulate,
                        TranportName = proxyTransport.Name,
                        RawEndpoint = startableEndpointProxy
                    };
                }
            }
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

    Dictionary<string, BridgeTransport> transportConfigurationMappings = [];
    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointDispatchers = [];
    readonly Dictionary<string, string> targetEndpointAddressMappings = [];
    readonly Dictionary<string, string> endpointAddressMappings = [];

    public class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public string TranportName;
        public IStartableRawEndpoint RawEndpoint;
    }
}