using System;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public static class BridgeComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithBridge<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        Action<BridgeConfiguration> bridgeConfigurationAction)
        where TContext : ScenarioContext
    {
        return scenario.WithComponent(new BridgeComponent<TContext>(bridgeConfigurationAction));
    }
}
