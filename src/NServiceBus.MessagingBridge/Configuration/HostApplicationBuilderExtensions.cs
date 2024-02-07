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

            _ = builder.Services.AddSingleton(sp => bridgeConfiguration.FinalizeConfiguration(sp.GetRequiredService<ILogger<BridgeConfiguration>>()));
            _ = builder.Services.AddSingleton(deferredLoggerFactory);
            _ = builder.Services.AddSingleton<IHostedService, BridgeHostedService>();
            _ = builder.Services.AddSingleton<IHostedService, HeartbeatSenderBackgroundService>();
            _ = builder.Services.AddSingleton<IStartableBridge, StartableBridge>();
            _ = builder.Services.AddSingleton<EndpointProxyFactory>();
            _ = builder.Services.AddSingleton<SubscriptionManager>();
            _ = builder.Services.AddSingleton<EndpointRegistry>();
            _ = builder.Services.AddSingleton<IEndpointRegistry>(sp => sp.GetRequiredService<EndpointRegistry>());
            _ = builder.Services.AddSingleton<IMessageShovel, MessageShovel>();

            return builder;
        }
    }
}
