namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Raw;

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
        var transports = configuration.TransportConfigurations;

        await endpointRegistry.ApplyMappings(transports, cancellationToken).ConfigureAwait(false);

        var stoppableEndpointProxies = new List<IStoppableRawEndpoint>();

        // now that all proxies are created we can
        // start them up to make messages start flowing across the transports
        foreach (var endpointRegistration in endpointRegistry.Registrations)
        {
            var stoppableRawEndpoint = await endpointRegistration.RawEndpoint.Start(cancellationToken)
                .ConfigureAwait(false);

            stoppableEndpointProxies.Add(stoppableRawEndpoint);
        }

        foreach (var endpointRegistration in endpointRegistry.Registrations)
        {
            await subscriptionManager.SubscribeToEvents(endpointRegistration.RawEndpoint, endpointRegistration.Endpoint, cancellationToken)
               .ConfigureAwait(false);
        }

        logger.LogInformation("Bridge startup complete");

        return new RunningBridge(stoppableEndpointProxies);
    }

    readonly FinalizedBridgeConfiguration configuration;
    readonly EndpointRegistry endpointRegistry;
    readonly SubscriptionManager subscriptionManager;
    readonly ILogger<StartableBridge> logger;
}
