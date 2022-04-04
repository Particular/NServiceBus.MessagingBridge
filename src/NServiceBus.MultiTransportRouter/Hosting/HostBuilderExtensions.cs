namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NServiceBus.Logging;
    using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

    /// <summary>
    /// Extension methods to configure TODO for the .NET Core generic host.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures the host to start the TODO
        /// </summary>
        public static IHostBuilder UseRouter(this IHostBuilder hostBuilder, Func<HostBuilderContext, MessageRouterConfiguration> routerConfigurationBuilder)
        {
            var deferredLoggerFactory = new DeferredLoggerFactory();
            LogManager.UseFactory(deferredLoggerFactory);

            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                var routerConfiguration = routerConfigurationBuilder(ctx);
                serviceCollection.AddSingleton<IHostedService>(serviceProvider => new RouterHostedService(
                    routerConfiguration,
                    serviceProvider.GetRequiredService<IConfiguration>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>(),
                    deferredLoggerFactory));
            });

            return hostBuilder;
        }
    }
}