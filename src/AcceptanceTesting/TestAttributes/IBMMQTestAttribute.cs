#pragma warning disable PS0024 // "I" in IBMMQ is from IBM, not an interface prefix
using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class IBMMQTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var connectionString = Environment.GetEnvironmentVariable("IBMMQ_CONNECTIONSTRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Ignoring because environment variable IBMMQ_CONNECTIONSTRING is not available");
        }
    }
}