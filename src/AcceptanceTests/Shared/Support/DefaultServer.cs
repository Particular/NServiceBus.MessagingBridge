using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;

public class DefaultServer : IEndpointSetupTemplate
{
#pragma warning disable PS0013 // Add a CancellationToken parameter type argument
    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
#pragma warning restore PS0013 // Add a CancellationToken parameter type argument
    {
        var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

        configuration.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());
        configuration.EnableInstallers();
        configuration.UseSerialization<SystemJsonSerializer>();

        var recoverability = configuration.Recoverability();
        recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
        recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
        configuration.SendFailedMessagesTo("error");

        configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

        var transportConfig = TestSuiteConfiguration.Current.CreateTransportConfiguration();

        await transportConfig.Configure(endpointConfiguration.EndpointName, configuration, runDescriptor.Settings, endpointConfiguration.PublisherMetadata);

        runDescriptor.OnTestCompleted(_ => transportConfig.Cleanup());

        await configurationBuilderCustomization(configuration);

        return configuration;
    }
}
