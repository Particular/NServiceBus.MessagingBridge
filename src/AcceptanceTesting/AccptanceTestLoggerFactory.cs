using Microsoft.Extensions.Logging;
using NServiceBus.AcceptanceTesting;

public class AccptanceTestLoggerFactory : ILoggerFactory
{
    public AccptanceTestLoggerFactory(ScenarioContext scenarioContext) => this.scenarioContext = scenarioContext;

    public void AddProvider(ILoggerProvider provider) => throw new System.NotImplementedException();
    public ILogger CreateLogger(string categoryName)
    {
        return new ScenarioContextLogger(categoryName, scenarioContext);
    }

    public void Dispose()
    {

    }

    readonly ScenarioContext scenarioContext;
}
