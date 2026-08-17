using NUnit.Framework;

[assembly: LearningTransportTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureNonDurableTransportTestExecution();
}