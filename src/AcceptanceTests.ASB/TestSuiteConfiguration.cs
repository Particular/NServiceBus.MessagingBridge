[assembly: AzureServiceBusTest]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureAzureServiceBusTransportTestExecution();
}