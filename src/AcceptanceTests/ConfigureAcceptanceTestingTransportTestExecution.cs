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
        var storageDir = Path.Combine(Path.GetTempPath(), "left", testRunId);

        return new AcceptanceTestingTransport { StorageLocation = storageDir };
    }

    public Task Cleanup(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}