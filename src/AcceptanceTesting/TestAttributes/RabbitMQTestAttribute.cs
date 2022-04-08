using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class RabbitMQTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Ignoring because environment variable RabbitMQConnectionString is not available");
        }
    }
}