namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Raw;

class StartableBridge(
    FinalizedBridgeConfiguration configuration,
    EndpointRegistry endpointRegistry,
    SubscriptionManager subscriptionManager,
    ILogger<StartableBridge> logger) : IStartableBridge
{
    public async Task<IStoppableBridge> Start(CancellationToken cancellationToken = default)
    {
        var stoppableEndpointProxies = new List<IStoppableRawEndpoint>();

        await foreach (var endpointRegistration in endpointRegistry.Initialize(configuration.TransportConfigurations, cancellationToken).ConfigureAwait(false))
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
}
