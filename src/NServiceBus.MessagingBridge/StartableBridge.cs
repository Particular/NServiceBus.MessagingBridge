using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Raw;

class StartableBridge : IStartableBridge
{
    public StartableBridge(
        FinalizedBridgeConfiguration configuration,
        EndpointRegistry endpointRegistry,
        SubscriptionManager subscriptionManager,
        ILogger<StartableBridge> logger)
    {
        this.configuration = configuration;
        this.endpointRegistry = endpointRegistry;
        this.subscriptionManager = subscriptionManager;
        this.logger = logger;
    }

    public async Task<IStoppableBridge> Start(CancellationToken cancellationToken = default)
    {
        var stoppableEndpointProxies = new List<IStoppableRawEndpoint>();

        foreach (var endpointRegistration in await endpointRegistry.Initialize(configuration.TransportConfigurations, cancellationToken).ConfigureAwait(false))
        {
            var stoppableRawEndpoint = await endpointRegistration.RawEndpoint.Start(cancellationToken)
                .ConfigureAwait(false);

            stoppableEndpointProxies.Add(stoppableRawEndpoint);

            // If the endpoint is null, it is a dispatcher endpoint and we don't need to subscribe to events
            if (endpointRegistration.Endpoint != null)
            {
                await subscriptionManager.SubscribeToEvents(endpointRegistration.RawEndpoint, endpointRegistration.Endpoint, cancellationToken)
                   .ConfigureAwait(false);
            }
        }

        logger.LogInformation("Bridge startup complete");

        return new RunningBridge(stoppableEndpointProxies);
    }

    readonly FinalizedBridgeConfiguration configuration;
    readonly EndpointRegistry endpointRegistry;
    readonly SubscriptionManager subscriptionManager;
    readonly ILogger<StartableBridge> logger;
}
