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
    //NOTE: This method cannot have a return type of IAsyncEnumerable as all the endpoints need to be configured on the bridge before the bridge can start processing messages. 
    public async Task<IEnumerable<ProxyRegistration>> Initialize(IReadOnlyCollection<BridgeTransport> transportConfigurations, CancellationToken cancellationToken = default)
    {
        // Assume that it is the number of endpoints that is more likely to scale up in size than the number of transports (typically only 2).
        // Therefore in cases where it might matter, it is more efficient to iterate over the transports multiple times.

        IList<ProxyRegistration> proxyRegistrations = [];

        await CreateAndRegisterDispatchers(transportConfigurations, proxyRegistrations, cancellationToken)
            .ConfigureAwait(false);

        var allEndpoints = transportConfigurations.SelectMany(
            transport => transport.Endpoints.Select(endpoint => (transport, endpoint)));

        foreach (var (transport, endpoint) in allEndpoints)
        {
            AddressMap.Add(transport, endpoint);

            var dispatcher = dispatchers[transport.Name];
            var queueAddress = new QueueAddress(endpoint.Name);
            var targetTransportAddress = dispatcher.ToTransportAddress(queueAddress);
            endpointAddressMappings[endpoint.Name] = endpoint.QueueAddress ?? targetTransportAddress;
            targetEndpointDispatchers[endpoint.Name] = new TargetEndpointDispatcher(transport.Name, dispatcher, targetTransportAddress);

            await CreateAndRegisterProxies(transport, endpoint, transportConfigurations, proxyRegistrations, cancellationToken)
                .ConfigureAwait(false);
        }

        return proxyRegistrations;
    }

    async Task CreateAndRegisterDispatchers(
        IReadOnlyCollection<BridgeTransport> transportConfigurations,
        IList<ProxyRegistration> proxyRegistrations,
        CancellationToken cancellationToken)
    {
        foreach (var transport in transportConfigurations)
        {
            var dispatcher = await EndpointProxyFactory.CreateDispatcher(transport, cancellationToken).ConfigureAwait(false);
            dispatchers.Add(transport.Name, dispatcher);
            proxyRegistrations.Add(new ProxyRegistration
            {
                Endpoint = null,
                RawEndpoint = dispatcher
            });
        }

        AddressMap = new AddressMap(dispatchers);
    }

    async Task CreateAndRegisterProxies(
        BridgeTransport targetTransport,
        BridgeEndpoint targetEndpoint,
        IReadOnlyCollection<BridgeTransport> transportConfigurations,
        IList<ProxyRegistration> proxyRegistrations,
        CancellationToken cancellationToken)
    {
        // Endpoint will need to be proxied on the other transports
        foreach (var proxyTransport in transportConfigurations)
        {
            if (proxyTransport.Name == targetTransport.Name)
            {
                continue;
            }

            var startableEndpointProxy = await endpointProxyFactory.CreateProxy(targetEndpoint, proxyTransport, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Proxy for endpoint {endpoint} created on {transport}", targetEndpoint.Name, proxyTransport.Name);

            proxyRegistrations.Add(new ProxyRegistration
            {
                Endpoint = targetEndpoint,
                RawEndpoint = startableEndpointProxy
            });
        }
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

    public IAddressMap AddressMap { get; private set; }

    public string GetEndpointAddress(string endpointName)
    {
        if (endpointAddressMappings.TryGetValue(endpointName, out var address))
        {
            return address;
        }

        var nearestMatch = endpointName.GetClosestMatch(endpointAddressMappings.Keys) ?? "(No mappings registered)";

        throw new Exception($"No address mapping could be found for endpoint: {endpointName}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {nearestMatch}");
    }

    readonly Dictionary<string, IStartableRawEndpoint> dispatchers = [];
    readonly Dictionary<string, TargetEndpointDispatcher> targetEndpointDispatchers = [];
    readonly Dictionary<string, string> endpointAddressMappings = [];

    public class ProxyRegistration
    {
        public BridgeEndpoint Endpoint;
        public IStartableRawEndpoint RawEndpoint;
    }
}