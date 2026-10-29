using NUnit.Framework;

[assembly: SqlServerTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureSqlServerTransportTestExecution();
}

