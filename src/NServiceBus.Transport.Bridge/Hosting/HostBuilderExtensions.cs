namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
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
            Action<BridgeConfiguration> routerConfigurationAction)
        {
            return hostBuilder.UseNServiceBusBridge((_, rc) => routerConfigurationAction(rc));
        }

        /// <summary>
        /// Configures the host to start the bridge
        /// </summary>
        public static IHostBuilder UseNServiceBusBridge(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, BridgeConfiguration> routerConfigurationAction)
        {
            var deferredLoggerFactory = new DeferredLoggerFactory();
            LogManager.UseFactory(deferredLoggerFactory);

            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                serviceCollection.AddSingleton(sp =>
                {
                    var routerConfiguration = new BridgeConfiguration();

                    routerConfigurationAction(hostBuilderContext, routerConfiguration);

                    return routerConfiguration;
                });
                serviceCollection.AddSingleton(deferredLoggerFactory);
                serviceCollection.AddSingleton<IHostedService, BridgeHostedService>();
                serviceCollection.AddSingleton<IStartableBridge, StartableBridge>();
                serviceCollection.AddTransient<EndpointProxy>();
            });

            return hostBuilder;
        }
    }
}