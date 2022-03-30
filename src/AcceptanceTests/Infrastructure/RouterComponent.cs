using System.Threading;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

class RouterComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    MessageRouterConfiguration routerConfiguration;

    public RouterComponent(MessageRouterConfiguration routerConfiguration) => this.routerConfiguration = routerConfiguration;

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    {
        return Task.FromResult<ComponentRunner>(new Runner(routerConfiguration));
    }

    class Runner : ComponentRunner
    {
        public Runner(MessageRouterConfiguration routerConfiguration) => this.routerConfiguration = routerConfiguration;

        public override string Name => "Router";

        public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
        {
            router = await routerConfiguration.Start(cancellationToken).ConfigureAwait(false);
        }

        public override Task Stop()
        {
            return router.Stop();
        }

        MessageRouterConfiguration routerConfiguration;
        RunningRouter router;
    }
}