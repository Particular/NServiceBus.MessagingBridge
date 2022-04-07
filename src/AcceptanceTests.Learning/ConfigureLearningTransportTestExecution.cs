using System;
using System.IO;
using System.Threading;
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
            Cleanup = (ct) => Cleanup(ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new LearningTransport { StorageDirectory = GetStorageDir() };
        endpointConfiguration.UseTransport(transportDefinition);

        return Cleanup;
    }

    Task Cleanup(CancellationToken cancellationToken)
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