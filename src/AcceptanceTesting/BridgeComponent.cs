using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public class BridgeComponent<TContext> : IComponentBehavior where TContext : ScenarioContext
{
    public BridgeComponent(Action<BridgeConfiguration> bridgeConfigurationAction) => this.bridgeConfigurationAction = bridgeConfigurationAction;

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    {
        return Task.FromResult<ComponentRunner>(new Runner(bridgeConfigurationAction, new AcceptanceTestLoggerFactory(run.ScenarioContext)));
    }

    readonly Action<BridgeConfiguration> bridgeConfigurationAction;

    class Runner : ComponentRunner
    {
        public Runner(Action<BridgeConfiguration> bridgeConfigurationAction, ILoggerFactory loggerFactory)
        {
            this.bridgeConfigurationAction = bridgeConfigurationAction;
            this.loggerFactory = loggerFactory;
        }

        public override string Name => "Bridge";

        public override async Task Start(CancellationToken cancellationToken = default)
        {
            var builder = Host.CreateApplicationBuilder();

            var bridgeConfiguration = new BridgeConfiguration();
            bridgeConfigurationAction(bridgeConfiguration);

            builder.UseNServiceBusBridge(bridgeConfiguration);

            builder.Services.AddSingleton(loggerFactory);
            builder.Services.RemoveAll(typeof(IMessageShovel));
            builder.Services.AddTransient<MessageShovel>();
            builder.Services.AddTransient<FakeShovel>();
            builder.Services.AddTransient<IMessageShovel, FakeShovel>();

            host = builder.Build();

            await host.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task Stop(CancellationToken cancellationToken = default)
        {
            if (host != null)
            {
                await host.StopAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        IHost host;

        readonly Action<BridgeConfiguration> bridgeConfigurationAction;
        readonly ILoggerFactory loggerFactory;
    }
}