[assembly: AzureStorageQueueTest]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureAzureStorageQueuesTransportTestExecution();
}