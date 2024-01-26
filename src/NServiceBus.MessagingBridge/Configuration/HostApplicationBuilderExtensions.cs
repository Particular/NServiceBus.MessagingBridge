namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus.Logging;
    using NServiceBus.MessagingBridge.Heartbeats;

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
            LogManager.UseFactory(deferredLoggerFactory);

            builder.Services.AddSingleton(sp => bridgeConfiguration.FinalizeConfiguration(sp.GetRequiredService<ILogger<BridgeConfiguration>>()));
            builder.Services.AddSingleton(deferredLoggerFactory);
            builder.Services.AddSingleton<IHostedService, BridgeHostedService>();
            builder.Services.AddSingleton<IHostedService, HeartbeatHostedService>();
            builder.Services.AddSingleton<IStartableBridge, StartableBridge>();
            builder.Services.AddSingleton<EndpointProxyFactory>();
            builder.Services.AddSingleton<SubscriptionManager>();
            builder.Services.AddSingleton<EndpointRegistry>();
            builder.Services.AddSingleton<IEndpointRegistry>(sp => sp.GetRequiredService<EndpointRegistry>());
            builder.Services.AddSingleton<IMessageShovel, MessageShovel>();

            return builder;
        }
    }
}
