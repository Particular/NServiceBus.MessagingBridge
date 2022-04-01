using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;
using NUnit.Framework;

public class DefaultTestServer : IEndpointSetupTemplate
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

        var transportDefinition = GetTestTransportDefinition();
        configuration.UseTransport(transportDefinition);

        runDescriptor.OnTestCompleted(_ =>
        {
            Directory.Delete(transportDefinition.StorageLocation, true);

            return Task.CompletedTask;
        });

        configurationBuilderCustomization(configuration);

        return Task.FromResult(configuration);
    }

    public static AcceptanceTestingTransport GetTestTransportDefinition()
    {
        var testRunId = TestContext.CurrentContext.Test.ID;
        //make sure to run in a non-default directory to not clash with learning transport and other acceptance tests
        var storageDir = Path.Combine(Path.GetTempPath(), "right", testRunId);

        return new AcceptanceTestingTransport { StorageLocation = storageDir };
    }
}
