using NUnit.Framework;

[assembly: AzureStorageQueueTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureAzureStorageQueuesTransportTestExecution();
}