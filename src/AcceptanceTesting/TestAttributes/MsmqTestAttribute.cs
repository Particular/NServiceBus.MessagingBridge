using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class MsmqTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var isGithubCI = Environment.GetEnvironmentVariable("CI") == "true";
        var connectionString = Environment.GetEnvironmentVariable("MSMQTransport");
        if (isGithubCI && string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Ignoring learning transport tests due to unset MSMQTransport environment variable");
        }
    }
}