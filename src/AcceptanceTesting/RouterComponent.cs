using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

partial class RouterComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    public RouterComponent(Action<RouterConfiguration> routerConfigurationAction) => this.routerConfigurationAction = routerConfigurationAction;

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    {
        return Task.FromResult<ComponentRunner>(new Runner(routerConfigurationAction, new AccptanceTestLoggerFactory(run.ScenarioContext)));
    }

    readonly Action<RouterConfiguration> routerConfigurationAction;

    class Runner : ComponentRunner
    {
        public Runner(Action<RouterConfiguration> routerConfigurationAction, ILoggerFactory loggerFactory)
        {
            this.routerConfigurationAction = routerConfigurationAction;
            this.loggerFactory = loggerFactory;
        }

        public override string Name => "Router";

        public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseNServiceBusBridge(routerConfigurationAction)
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

        readonly Action<RouterConfiguration> routerConfigurationAction;
        readonly ILoggerFactory loggerFactory;
    }
}