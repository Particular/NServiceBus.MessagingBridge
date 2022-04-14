using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus.Raw;

class StartableBridge : IStartableBridge
{
    public StartableBridge(
        FinalizedBridgeConfiguration configuration,
        EndpointProxyFactory endpointProxyFactory,
        EndpointRegistry endpointRegistry,
        SubscriptionManager subscriptionManager,
        ILogger<StartableBridge> logger)
    {
        this.configuration = configuration;
        this.endpointProxyFactory = endpointProxyFactory;
        this.endpointRegistry = endpointRegistry;
        this.subscriptionManager = subscriptionManager;
        this.logger = logger;
    }

    public async Task<IStoppableBridge> Start(CancellationToken cancellationToken = default)
    {
        var transports = configuration.TransportConfigurations;
        var startableEndpointProxies = new List<IStartableRawEndpoint>();

        // create required proxy endpoints on all transports
        foreach (var transportConfiguration in transports)
        {
            logger.LogInformation("Creating proxies for transport {name}", transportConfiguration.Name);

            // get all endpoints that we need to proxy in this transport, ie all that don't exist this transport.
            var endpoints = transports.Where(s => s != transportConfiguration).SelectMany(s => s.Endpoints);

            // create the proxy and subscribe it to configured events
            foreach (var endpointToSimulate in endpoints)
            {
                var startableEndpointProxy = await endpointProxyFactory.CreateProxy(
                   endpointToSimulate,
                   transportConfiguration,
                   cancellationToken)
                   .ConfigureAwait(false);

                await subscriptionManager.SubscribeToEvents(startableEndpointProxy, endpointToSimulate.Subscriptions, cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation("Proxy for endpoint {endpoint} created on {transport}", endpointToSimulate.Name, transportConfiguration.Name);

                startableEndpointProxies.Add(startableEndpointProxy);

                endpointRegistry.RegisterDispatcher(endpointToSimulate, transportConfiguration.Name, startableEndpointProxy);
            }
        }

        endpointRegistry.ApplyMappings(transports);

        var stoppableEndpointProxies = new List<IStoppableRawEndpoint>();

        // now that all proxies are created and subscriptions are setup we can
        // start them up to make messages start flowing across the transports
        foreach (var endpointProxy in startableEndpointProxies)
        {
            var stoppableRawEndpoint = await endpointProxy.Start(cancellationToken)
                .ConfigureAwait(false);

            stoppableEndpointProxies.Add(stoppableRawEndpoint);
        }

        logger.LogInformation("Bridge startup complete");

        return new RunningBridge(stoppableEndpointProxies);
    }

    readonly FinalizedBridgeConfiguration configuration;
    readonly EndpointProxyFactory endpointProxyFactory;
    readonly EndpointRegistry endpointRegistry;
    readonly SubscriptionManager subscriptionManager;
    readonly ILogger<StartableBridge> logger;
}
