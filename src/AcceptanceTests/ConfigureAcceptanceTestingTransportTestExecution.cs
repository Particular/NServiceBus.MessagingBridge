using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

class ConfigureAcceptanceTestingTransportTestExecution : IConfigureTransportTestExecution
{
    public TransportDefinition GetTransportDefinition()
    {
        var testRunId = TestContext.CurrentContext.Test.ID;
        //make sure to run in a non-default directory to not clash with learning transport and other acceptance tests
        storageDir = Path.Combine(Path.GetTempPath(), testRunId, "left");

        return new AcceptanceTestingTransport { StorageLocation = storageDir };
    }

    public void ApplyCustomEndpointConfiguration(EndpointConfiguration endpointConfiguration)
    {
        //no-op
    }

    public Task Cleanup(CancellationToken cancellationToken = default)
    {
        Directory.Delete(storageDir, true);
        return Task.CompletedTask;
    }

    string storageDir;
}