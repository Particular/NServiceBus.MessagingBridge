using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public static class ConfigureExtensions
{
    public static RoutingSettings ConfigureRouting(this EndpointConfiguration configuration) =>
        new RoutingSettings(configuration.GetSettings());
}