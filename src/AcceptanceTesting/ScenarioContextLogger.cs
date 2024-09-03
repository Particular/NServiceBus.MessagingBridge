using System;
using Microsoft.Extensions.Logging;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class ScenarioContextLogger(string categoryName, ScenarioContext scenarioContext) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var log = $"{categoryName}: {formatter(state, exception)} - {exception}";

        if (logLevel >= LogLevel.Warning)
        {
            TestContext.WriteLine(log);
        }

        scenarioContext.AddTrace(log);
    }
}