using NUnit.Framework;

[assembly: AzureServiceBusTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureAzureServiceBusTransportTestExecution();
}