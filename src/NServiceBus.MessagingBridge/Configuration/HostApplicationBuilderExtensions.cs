namespace NServiceBus;

using System;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods to configure the bridge for the .NET hosted applications builder.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the host to start the bridge.
    /// </summary>
    public static IHostApplicationBuilder UseNServiceBusBridge(this IHostApplicationBuilder builder, BridgeConfiguration bridgeConfiguration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(bridgeConfiguration);

        var deferredLoggerFactory = new DeferredLoggerFactory();
#pragma warning disable CS0618 // Type or member is obsolete
        LogManager.UseFactory(deferredLoggerFactory);
#pragma warning restore CS0618 // Type or member is obsolete

        _ = builder.Services.AddSingleton(sp => bridgeConfiguration.FinalizeConfiguration(sp.GetRequiredService<ILogger<BridgeConfiguration>>()))
            .AddSingleton(deferredLoggerFactory)
            .AddHostedService<BridgeHostedService>()
            .AddSingleton<IStartableBridge, StartableBridge>()
            .AddSingleton<EndpointProxyFactory>()
            .AddSingleton<SubscriptionManager>()
            .AddSingleton<EndpointRegistry>()
            .AddSingleton<IEndpointRegistry>(sp => sp.GetRequiredService<EndpointRegistry>())
            .AddSingleton<IMessageShovel, MessageShovel>()
            .AddHostedService<HeartbeatSenderBackgroundService>()
            .AddHostedService<CustomChecksBackgroundService>()
            .AddCustomChecks();

        return builder;
    }
}