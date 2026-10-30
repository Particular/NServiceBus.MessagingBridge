using NUnit.Framework;

[assembly: AmazonSQSTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureSQSTransportTestExecution();
}