using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;

public class DefaultServer : IEndpointSetupTemplate
{
    public virtual Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
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

        var transportCleanup = transportConfig.ConfigureTransportForEndpoint(configuration, endpointConfiguration.PublisherMetadata);

        runDescriptor.OnTestCompleted(_ => transportCleanup(CancellationToken.None));

        configurationBuilderCustomization(configuration);

        return Task.FromResult(configuration);
    }
}
