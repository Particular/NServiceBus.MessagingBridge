using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public class BridgeComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    public BridgeComponent(Action<BridgeConfiguration> bridgeConfigurationAction) => this.bridgeConfigurationAction = bridgeConfigurationAction;

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    {
        return Task.FromResult<ComponentRunner>(new Runner(bridgeConfigurationAction, new AccptanceTestLoggerFactory(run.ScenarioContext)));
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
            var hostBuilder = new HostBuilder();

            hostBuilder.UseNServiceBusBridge(bridgeConfigurationAction)
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddSingleton(loggerFactory);
                });

            host = await hostBuilder.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            if (host != null)
            {
                await host.StopAsync().ConfigureAwait(false);
            }
        }

        IHost host;

        readonly Action<BridgeConfiguration> bridgeConfigurationAction;
        readonly ILoggerFactory loggerFactory;
    }
}