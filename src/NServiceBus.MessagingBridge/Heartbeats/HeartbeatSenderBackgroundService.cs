
#nullable enable
namespace NServiceBus.MessagingBridge.Heartbeats;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using Microsoft.Extensions.Hosting;
using Raw;

class HeartbeatSenderBackgroundService(FinalizedBridgeConfiguration finalizedBridgeConfiguration) : BackgroundService
{

    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (var heartbeatSender in heartbeatSenders)
        {
            await heartbeatSender.Stop(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var transportConfiguration in finalizedBridgeConfiguration.TransportConfigurations)
        {
            if (transportConfiguration.HeartbeatConfiguration != null)
            {
                var heartbeatSender = await ConfigureHeartbeatSenderForTransport(transportConfiguration, cancellationToken).ConfigureAwait(false);

                await heartbeatSender.Start(cancellationToken).ConfigureAwait(false);

                heartbeatSenders.Add(heartbeatSender);
            }
        }
    }

    static async Task<HeartbeatSender> ConfigureHeartbeatSenderForTransport(BridgeTransport transportConfiguration, CancellationToken cancellationToken)
    {
        var endpointName = $"MessagingBridge.{transportConfiguration.Name}"; // MessageBridge.TransportName

        var rawEndpointConfiguration = RawEndpointConfiguration.CreateSendOnly(endpointName, transportConfiguration.TransportDefinition);

        // If this isn't called, errors are thrown due to missing .Delayed queue for delayed delivery functionality.
        rawEndpointConfiguration.AutoCreateQueues();

        var sendOnlyHeartbeatEndpoint = await RawEndpoint.Create(rawEndpointConfiguration, cancellationToken).ConfigureAwait(false);

        //receiveAddress is null because the heartbeat endpoint is send only
        var serviceControlBackEnd = new ServiceControlBackend(
            transportConfiguration.HeartbeatConfiguration.ServiceControlQueue,
            receiveAddresses: null);

        var heartBeatSender = new HeartbeatSender(
            sendOnlyHeartbeatEndpoint,
            GetHostInformation(),
            serviceControlBackEnd,
            endpointName,
            transportConfiguration.HeartbeatConfiguration.Frequency,
            transportConfiguration.HeartbeatConfiguration.TimeToLive);

        return heartBeatSender;
    }

    static HostInformation GetHostInformation()
    {
        var displayName = Environment.MachineName;
        var hostId = DeterministicGuid.Create(displayName, PathUtilities.SanitizedPath(Environment.CommandLine));

        return new HostInformation(hostId, displayName);
    }

    readonly List<HeartbeatSender> heartbeatSenders = [];
}