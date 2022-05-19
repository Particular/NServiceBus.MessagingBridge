using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class DefaultTestServer : IEndpointSetupTemplate
{
#pragma warning disable PS0013 // Add a CancellationToken parameter type argument
    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
#pragma warning restore PS0013 // Add a CancellationToken parameter type argument
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
            if (Directory.Exists(transportDefinition.StorageLocation))
            {
                Directory.Delete(transportDefinition.StorageLocation, true);
            }

            return Task.CompletedTask;
        });

        await configurationBuilderCustomization(configuration);

        return configuration;
    }

    public static AcceptanceTestingTransport GetTestTransportDefinition()
    {
        var testRunId = TestContext.CurrentContext.Test.ID;
        //make sure to run in a non-default directory to not clash with learning transport and other acceptance tests
        var storageDir = Path.Combine(Path.GetTempPath(), testRunId, "DefaultTestingTransport");

        return new AcceptanceTestingTransport { StorageLocation = storageDir };
    }
}
