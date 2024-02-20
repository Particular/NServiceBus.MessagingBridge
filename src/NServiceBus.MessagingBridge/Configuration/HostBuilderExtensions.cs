namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Logging;

/// <summary>
/// Extension methods to configure the bridge for the .NET generic host.
/// </summary>
[SuppressMessage("Style", "IDE0058:Expression value is never used")]
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the host to start the bridge.
    /// </summary>
    public static IHostBuilder UseNServiceBusBridge(this IHostBuilder hostBuilder,
        Action<BridgeConfiguration> bridgeConfigurationAction)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(bridgeConfigurationAction);

        return hostBuilder.UseNServiceBusBridge((_, rc) => bridgeConfigurationAction(rc));
    }

    /// <summary>
    /// Configures the host to start the bridge.
    /// </summary>
    public static IHostBuilder UseNServiceBusBridge(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, BridgeConfiguration> bridgeConfigurationAction)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(bridgeConfigurationAction);

        var deferredLoggerFactory = new DeferredLoggerFactory();
        LogManager.UseFactory(deferredLoggerFactory);

        hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
        {
            var bridgeConfiguration = new BridgeConfiguration();

            bridgeConfigurationAction(hostBuilderContext, bridgeConfiguration);

            serviceCollection.AddSingleton(sp => bridgeConfiguration.FinalizeConfiguration(
                    sp.GetRequiredService<ILogger<BridgeConfiguration>>()))
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
        });

        return hostBuilder;
    }
}