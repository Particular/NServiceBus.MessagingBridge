[assembly: LearningTransportTest]
public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureLearningTransportTestExecution();
}

