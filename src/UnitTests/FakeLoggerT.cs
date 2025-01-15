namespace UnitTests;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

class FakeLogger<T> : ILogger<T>
{
    public readonly List<string> logEntries = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        logEntries.Add(state.ToString());
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => throw new NotSupportedException();
}