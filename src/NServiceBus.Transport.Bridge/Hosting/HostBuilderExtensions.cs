namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus.Logging;
    using NServiceBus.Transport.Bridge;

    /// <summary>
    /// Extension methods to configure the bridge for the .NET Core generic host.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures the host to start the bridge
        /// </summary>
        public static IHostBuilder UseNServiceBusBridge(
            this IHostBuilder hostBuilder,
            Action<BridgeConfiguration> bridgeConfigurationAction)
        {
            return hostBuilder.UseNServiceBusBridge((_, rc) => bridgeConfigurationAction(rc));
        }

        /// <summary>
        /// Configures the host to start the bridge
        /// </summary>
        public static IHostBuilder UseNServiceBusBridge(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, BridgeConfiguration> bridgeConfigurationAction)
        {
            var deferredLoggerFactory = new DeferredLoggerFactory();
            LogManager.UseFactory(deferredLoggerFactory);

            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var bridgeConfiguration = new BridgeConfiguration();

                bridgeConfigurationAction(hostBuilderContext, bridgeConfiguration);

                serviceCollection.AddSingleton(sp =>
                {
                    bridgeConfiguration.FinalizeConfiguration(sp.GetRequiredService<ILogger<BridgeConfiguration>>());
                    return bridgeConfiguration;
                });

                serviceCollection.AddSingleton(deferredLoggerFactory);
                serviceCollection.AddSingleton<IHostedService, BridgeHostedService>();
                serviceCollection.AddSingleton<IStartableBridge, StartableBridge>();
                serviceCollection.AddSingleton<EndpointProxyFactory>();
                serviceCollection.AddSingleton<SubscriptionManager>();
                serviceCollection.AddSingleton<EndpointProxyRegistry>();
                serviceCollection.AddSingleton<ITargetEndpointProxyRegistry>(sp => sp.GetRequiredService<EndpointProxyRegistry>());
                serviceCollection.AddTransient<MessageShovel>();
            });

            return hostBuilder;
        }
    }
}