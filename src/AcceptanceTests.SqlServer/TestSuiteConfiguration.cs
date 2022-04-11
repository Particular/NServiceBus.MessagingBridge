[assembly: SqlServerTest]
public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureSqlServerTransportTestExecution();
}

