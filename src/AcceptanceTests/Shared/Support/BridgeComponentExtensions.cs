using System;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public static class BridgeComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithBridge<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        Action<BridgeConfiguration, BridgeTransport> bridgeConfigurationAction, Action<PublisherMetadata> publisherMetadataAction = null)
        where TContext : BridgeScenarioContext =>
        scenario.WithComponent(new BridgeComponent<TContext>(bridgeConfigurationAction, publisherMetadataAction, TestSuiteConfiguration.Current.CreateTransportConfiguration()));
}
