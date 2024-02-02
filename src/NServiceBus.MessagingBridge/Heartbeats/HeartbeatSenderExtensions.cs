
#nullable enable

namespace NServiceBus.MessagingBridge.Heartbeats;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raw;

/// <summary>
/// 
/// </summary>
public static class HeartbeatSenderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddHeartBeatSender(this IServiceCollection services)
    {
        services.AddHostedService<HeartbeatSenderBackgroundService>();

        return services;
    }
}

/// <summary>
/// 
/// </summary>
class HeartbeatSenderBackgroundService : BackgroundService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="finalizedBridgeConfiguration"></param>
    public HeartbeatSenderBackgroundService(FinalizedBridgeConfiguration finalizedBridgeConfiguration)
    {
        this.finalizedBridgeConfiguration = finalizedBridgeConfiguration;
        heartbeatSenders = [];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var transportConfigurationsWithHeartbeats =
            finalizedBridgeConfiguration
                .TransportConfigurations
                .Where(t => t.HeartbeatConfiguration != null);

        foreach(var transportConfiguration in transportConfigurationsWithHeartbeats)
        {
            var endpointName = "bridge";//MessageBridge.TransportName 

            var rawEndpointConfiguration =
                RawEndpointConfiguration.CreateSendOnly(endpointName, transportConfiguration.TransportDefinition);

            var sendOnlyHeartbeatEndpoint = await RawEndpoint.Create(rawEndpointConfiguration, cancellationToken).ConfigureAwait(false);

            var displayName = Environment.MachineName;

            var hostId = DeterministicGuid.Create(displayName,PathUtilities.SanitizedPath(Environment.CommandLine));

            var hostInformation = new HostInformation(hostId,displayName);

            //receiveAddress is null because the heartbeat endpoint is send only
            var serviceControlBackEnd = new ServiceControlBackend(
                transportConfiguration.HeartbeatConfiguration.ServiceControlQueue,
                null);

            var heartBeatSender = new HeartbeatSender(
                sendOnlyHeartbeatEndpoint,
                hostInformation,
                serviceControlBackEnd,
                endpointName,
                transportConfiguration.HeartbeatConfiguration.Frequency,
                transportConfiguration.HeartbeatConfiguration.TimeToLive);

            heartbeatSenders.Add(heartBeatSender);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        //loop and start HeartbeatServices 
        foreach (var heartbeatSender in heartbeatSenders)
        {
            await heartbeatSender.Start(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var heartbeatSender in heartbeatSenders)
        {
            await heartbeatSender.Stop(cancellationToken).ConfigureAwait(false);
        }
    }

    FinalizedBridgeConfiguration finalizedBridgeConfiguration;
    List<HeartbeatSender> heartbeatSenders;
}