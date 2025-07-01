using NServiceBus;
using NServiceBus.AcceptanceTesting;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public static class BridgeConfigurationExtensions
{
    public static void AddTestEndpoint<T>(this BridgeTransport bridgeTransportConfiguration)
        where T : EndpointConfigurationBuilder =>
        bridgeTransportConfiguration.HasEndpoint(Conventions.EndpointNamingConvention(typeof(T)));

    public static void AddTestTransportEndpoint<T>(this BridgeConfiguration bridgeConfiguration)
         where T : EndpointConfigurationBuilder =>
        bridgeConfiguration.AddTestTransportEndpoint(new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(T))));

    public static void AddTestTransportEndpoint(this BridgeConfiguration bridgeConfiguration, BridgeEndpoint bridgeEndpoint)
    {
        var bridgeTransport = DefaultTestServer.GetTestTransportDefinition().ToTestableBridge("DefaultTestingTransport");

        bridgeTransport.HasEndpoint(bridgeEndpoint);

        bridgeConfiguration.AddTransport(bridgeTransport);
    }
}
