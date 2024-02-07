namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus.Logging;
    using NServiceBus.MessagingBridge.Heartbeats;

    /// <summary>
    /// Extension methods to configure the bridge for the .NET generic host.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures the host to start the bridge.
        /// </summary>
        public static IHostBuilder UseNServiceBusBridge(this IHostBuilder hostBuilder, Action<BridgeConfiguration> bridgeConfigurationAction)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            ArgumentNullException.ThrowIfNull(bridgeConfigurationAction);

            return hostBuilder.UseNServiceBusBridge((_, rc) => bridgeConfigurationAction(rc));
        }

        /// <summary>
        /// Configures the host to start the bridge.
        /// </summary>
        public static IHostBuilder UseNServiceBusBridge(this IHostBuilder hostBuilder, Action<HostBuilderContext, BridgeConfiguration> bridgeConfigurationAction)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            ArgumentNullException.ThrowIfNull(bridgeConfigurationAction);

            var deferredLoggerFactory = new DeferredLoggerFactory();
            LogManager.UseFactory(deferredLoggerFactory);

            _ = hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var bridgeConfiguration = new BridgeConfiguration();

                bridgeConfigurationAction(hostBuilderContext, bridgeConfiguration);

                _ = serviceCollection.AddSingleton(sp => bridgeConfiguration.FinalizeConfiguration(sp.GetRequiredService<ILogger<BridgeConfiguration>>()));

                _ = serviceCollection.AddSingleton(deferredLoggerFactory);
                _ = serviceCollection.AddSingleton<IHostedService, BridgeHostedService>();
                _ = serviceCollection.AddSingleton<IHostedService, HeartbeatSenderBackgroundService>();
                _ = serviceCollection.AddSingleton<IStartableBridge, StartableBridge>();
                _ = serviceCollection.AddSingleton<EndpointProxyFactory>();
                _ = serviceCollection.AddSingleton<SubscriptionManager>();
                _ = serviceCollection.AddSingleton<EndpointRegistry>();
                _ = serviceCollection.AddSingleton<IEndpointRegistry>(sp => sp.GetRequiredService<EndpointRegistry>());
                _ = serviceCollection.AddSingleton<IMessageShovel, MessageShovel>();
            });

            return hostBuilder;
        }
    }
}