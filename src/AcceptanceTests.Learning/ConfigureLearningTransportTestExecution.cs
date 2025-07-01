using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

class ConfigureLearningTransportTestExecution : IConfigureTransportTestExecution
{
    LearningTransport transportDefinition;

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        transportDefinition = new LearningTransport { StorageDirectory = GetStorageDir() };
        endpointConfiguration.UseTransport(transportDefinition);
        return Task.CompletedTask;
    }

    public Task Cleanup() => Cleanup(transportDefinition);

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new LearningTransport { StorageDirectory = GetStorageDir() }.ToTestableBridge();

    public Task Cleanup(BridgeTransport bridgeTransport) => Cleanup(bridgeTransport.FromTestableBridge<LearningTransport>());

    static Task Cleanup(LearningTransport transport)
    {
        var storageDir = transport.StorageDirectory;

        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, true);
        }

        return Task.CompletedTask;
    }

    static string GetStorageDir()
    {
        var testRunId = TestContext.CurrentContext.Test.ID;
        //make sure to run in a non-default directory to not clash with learning transport and other acceptance tests
        return Path.Combine(Path.GetTempPath(), testRunId, "learning");
    }
}