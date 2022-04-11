using Microsoft.Extensions.Logging;
using NServiceBus.AcceptanceTesting;

public class AcceptanceTestLoggerFactory : ILoggerFactory
{
    public AcceptanceTestLoggerFactory(ScenarioContext scenarioContext) => this.scenarioContext = scenarioContext;

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
