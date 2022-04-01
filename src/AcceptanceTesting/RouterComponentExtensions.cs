using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public static class RouterComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithRouter<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        MessageRouterConfiguration routerConfiguration)
        where TContext : ScenarioContext
    {
        return scenario.WithComponent(new RouterComponent<TContext>(routerConfiguration));
    }
}
