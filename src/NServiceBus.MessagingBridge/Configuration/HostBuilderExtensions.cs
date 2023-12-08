namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus.Logging;

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

            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var bridgeConfiguration = new BridgeConfiguration();

                bridgeConfigurationAction(hostBuilderContext, bridgeConfiguration);

                serviceCollection.AddSingleton(sp => bridgeConfiguration.FinalizeConfiguration(sp.GetRequiredService<ILogger<BridgeConfiguration>>()));

                serviceCollection.AddSingleton(deferredLoggerFactory);
                serviceCollection.AddSingleton<IHostedService, BridgeHostedService>();
                serviceCollection.AddSingleton<IStartableBridge, StartableBridge>();
                serviceCollection.AddSingleton<EndpointProxyFactory>();
                serviceCollection.AddSingleton<SubscriptionManager>();
                serviceCollection.AddSingleton<EndpointRegistry>();
                serviceCollection.AddSingleton<IEndpointRegistry>(sp => sp.GetRequiredService<EndpointRegistry>());
                serviceCollection.AddSingleton<IMessageShovel, MessageShovel>();
            });

            return hostBuilder;
        }
    }
}