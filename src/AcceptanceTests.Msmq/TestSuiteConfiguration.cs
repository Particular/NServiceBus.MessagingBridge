[assembly: MsmqTest]
public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureMsmqTransportTestExecution();
}