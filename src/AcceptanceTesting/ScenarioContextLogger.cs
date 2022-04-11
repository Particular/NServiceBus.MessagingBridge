using System;
using Microsoft.Extensions.Logging;
using NServiceBus.AcceptanceTesting;

public class ScenarioContextLogger : ILogger
{
    public ScenarioContextLogger(string categoryName, ScenarioContext scenarioContext)
    {
        this.categoryName = categoryName;
        this.scenarioContext = scenarioContext;
    }

    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        scenarioContext.AddTrace(categoryName + ": " + formatter(state, exception));
    }

    readonly string categoryName;
    readonly ScenarioContext scenarioContext;
}
