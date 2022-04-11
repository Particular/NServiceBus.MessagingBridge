using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class AmazonSQSTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var connectionString = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Ignoring because environment variable AWS_ACCESS_KEY_ID is not available");
        }
    }
}