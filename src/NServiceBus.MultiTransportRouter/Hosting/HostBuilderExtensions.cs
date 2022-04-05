namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NServiceBus.Logging;

    /// <summary>
    /// Extension methods to configure TODO for the .NET Core generic host.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures the host to start the TODO
        /// </summary>
        public static IHostBuilder UseRouter(this IHostBuilder hostBuilder, Action<RouterConfiguration> routerConfigurationAction)
        {
            var deferredLoggerFactory = new DeferredLoggerFactory();
            LogManager.UseFactory(deferredLoggerFactory);

            hostBuilder.ConfigureServices((_, serviceCollection) =>
            {
                var routerConfiguration = new RouterConfiguration();

                routerConfigurationAction(routerConfiguration);

                serviceCollection.AddSingleton(sp =>
                {
                    return routerConfiguration.Finalize(sp.GetRequiredService<IConfiguration>());
                });
                serviceCollection.AddSingleton(deferredLoggerFactory);
                serviceCollection.AddSingleton<IHostedService, RouterHostedService>();
                serviceCollection.AddSingleton<StartableRouter>();
                serviceCollection.AddTransient<EndpointProxy>();
            });

            return hostBuilder;
        }
    }
}