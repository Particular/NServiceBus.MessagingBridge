using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

class ConfigureLearningTransportTestExecution : IConfigureTransportTestExecution
{
    public BridgeTransportDefinition GetBridgeTransport()
    {
        return new BridgeTransportDefinition
        {
            TransportDefinition = new LearningTransport { StorageDirectory = GetStorageDir() },
            Cleanup = _ => Cleanup()
        };
    }

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new LearningTransport { StorageDirectory = GetStorageDir() };
        endpointConfiguration.UseTransport(transportDefinition);
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        var storageDir = GetStorageDir();

        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, true);
        }

        return Task.CompletedTask;
    }

    string GetStorageDir()
    {
        var testRunId = TestContext.CurrentContext.Test.ID;
        //make sure to run in a non-default directory to not clash with learning transport and other acceptance tests
        return Path.Combine(Path.GetTempPath(), testRunId, "learning");
    }
}