using NUnit.Framework;

[assembly: IBMMQTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureIBMMQTransportTestExecution();
}
