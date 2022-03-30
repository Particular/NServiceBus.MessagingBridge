using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvancedExtensibility;

public static class ConfigureExtensions
{
    public static RoutingSettings ConfigureRouting(this EndpointConfiguration configuration) =>
        new RoutingSettings(configuration.GetSettings());

    //// This is kind of a hack because the acceptance testing framework doesn't give any access to the transport definition to individual tests.
    //public static TransportDefinition ConfigureTransport(this EndpointConfiguration configuration) =>
    //    configuration.GetSettings().Get<TransportDefinition>();

    //public static TTransportDefinition ConfigureTransport<TTransportDefinition>(
    //    this EndpointConfiguration configuration)
    //    where TTransportDefinition : TransportDefinition =>
    //    (TTransportDefinition)configuration.GetSettings().Get<TransportDefinition>();

    public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
    {
        builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });
    }

    static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IServiceCollection r)
    {
        var type = runDescriptor.ScenarioContext.GetType();
        while (type != typeof(object))
        {
            r.AddSingleton(type, runDescriptor.ScenarioContext);
            type = type.BaseType;
        }
    }
}