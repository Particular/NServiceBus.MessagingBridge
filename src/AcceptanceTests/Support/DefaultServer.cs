using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;

public class DefaultServer : IEndpointSetupTemplate
{
#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public virtual Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    {
        var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

        configuration.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());
        configuration.EnableInstallers();

        var recoverability = configuration.Recoverability();
        recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
        recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
        configuration.SendFailedMessagesTo("error");

        configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

        var transportConfig = TestSuiteConfiguration.Current.CreateTransportConfiguration();

        var transportDefinition = transportConfig.GetTransportDefinition();

        configuration.UseTransport(transportDefinition);

        runDescriptor.OnTestCompleted(_ => transportConfig.Cleanup());

        configurationBuilderCustomization(configuration);

        return Task.FromResult(configuration);
    }
}
