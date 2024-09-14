using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;

class EndpointRegistry(EndpointProxyFactory endpointProxyFactory, ILogger<StartableBridge> logger) : IEndpointRegistry
{
    //NOTE: This method cannot have a return type of IAsyncEnumerable as all the endpoints need to be configured on the bridge before the bridge can start processing messages. 
    public async Task<IEnumerable<ProxyRegistration>> Initialize(IReadOnlyCollection<BridgeTransport> transportConfigurations, CancellationToken cancellationToken = default)
    {
        // Assume that it is the number of endpoints that is more likely to scale up in size than the number of transports (typically only 2).
        // Therefore in cases where it might matter, it is more efficient to iterate over the transports multiple times.

        IList<ProxyRegistration> proxyRegistrations = [];

        foreach (var targetTransport in transportConfigurations)
        {
            // Create the dispatcher for this transport
            var dispatchEndpoint = await EndpointProxyFactory.CreateDispatcher(targetTransport, cancellationToken).ConfigureAwait(false);

            proxyRegistrations.Add(new ProxyRegistration
            {
                Endpoint = null,
                TranportName = targetTransport.Name,
                RawEndpoint = dispatchEndpoint
            });

            // create required proxy endpoints on all transports
            foreach (var endpointToSimulate in targetTransport.Endpoints)
            {
                var queueAddress = new QueueAddress(endpointToSimulate.Name);
                var targetTransportAddress = dispatchEndpoint.ToTransportAddress(queueAddress);
                endpointAddressMappings[endpointToSimulate.Name] = endpointToSimulate.QueueAddress ?? targetTransportAddress;
                targetEndpointDispatchers[endpointToSimulate.Name] = new TargetEndpointDispatcher(targetTransport.Name, dispatchEndpoint, targetTransportAddress);

                // Endpoint will need to be proxied on the other transports
                foreach (var proxyTransport in transportConfigurations)
                {
                    string sourceTransportAddress = null;

                    if (proxyTransport.Name != targetTransport.Name)
                    {
                        var startableEndpointProxy = await endpointProxyFactory.CreateProxy(endpointToSimulate, proxyTransport, cancellationToken)
                            .ConfigureAwait(false);

                        logger.LogInformation("Proxy for endpoint {endpoint} created on {transport}", endpointToSimulate.Name, proxyTransport.Name);

                        sourceTransportAddress = startableEndpointProxy.ToTransportAddress(queueAddress);

                        proxyRegistrations.Add(new ProxyRegistration
                        {
                            Endpoint = endpointToSimulate,
                            TranportName = proxyTransport.Name,
                            RawEndpoint = startableEndpointProxy
                        });
                    }

                    AddressMap.Add(proxyTransport.Name, targetTransportAddress, sourceTransportAddress ?? targetTransportAddress);
                }
            }
        }

        return proxyRegistrations;
    }

    public TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName)
    {
        if (targetEndpointDispatchers.TryGetValue(sourceEndpointName, out var endpointDispatcher))
        {
            return endpointDispatcher;
        }

        var nearestMatch = sourceEndpointName.GetClosestMatch(targetEndpointDispatchers.Keys);

        throw new Exception($"No target endpoint dispatcher could be found for endpoint: {sourceEndpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    public IAddressMap AddressMap { get; } = new AddressMap();

    public string GetEndpointAddress(string endpointName)
    {
        if (endpointAddressMappings.TryGetValue(endpointName, out var address))
        {
            return address;
        }

        var nearestMatch = endpointName.GetClosestMatch(endpointAddressMappings.Keys) ?? "(No mappings registered)";

        throw new Exception($"No address mapping could be found for endpoint: {endpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointDispatchers = [];
    readonly Dictionary<string, string> endpointAddressMappings = [];

    public class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public string TranportName;
        public IStartableRawEndpoint RawEndpoint;
    }
}