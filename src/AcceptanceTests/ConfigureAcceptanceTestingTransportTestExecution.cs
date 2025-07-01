using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

class ConfigureAcceptanceTestingTransportTestExecution : IConfigureTransportTestExecution
{
    AcceptanceTestingTransport transportDefinition;

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        transportDefinition = new AcceptanceTestingTransport { StorageLocation = GetStorageDir() };
        endpointConfiguration.UseTransport(transportDefinition);
        return Task.CompletedTask;
    }

    public Task Cleanup() => Cleanup(transportDefinition);

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new AcceptanceTestingTransport { StorageLocation = GetStorageDir() }.ToTestableBridge();

    public Task Cleanup(BridgeTransport bridgeTransport) => Cleanup(bridgeTransport.FromTestableBridge<AcceptanceTestingTransport>());

    static Task Cleanup(AcceptanceTestingTransport transport)
    {
        var storageDir = transport.StorageLocation;

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
        return Path.Combine(Path.GetTempPath(), testRunId, "left");
    }
}