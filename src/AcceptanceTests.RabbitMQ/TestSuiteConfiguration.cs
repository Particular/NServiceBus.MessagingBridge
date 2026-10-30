using NUnit.Framework;

[assembly: RabbitMQTest]
[assembly: Parallelizable(ParallelScope.Fixtures)]

public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureRabbitMQTransportTestExecution();
}

