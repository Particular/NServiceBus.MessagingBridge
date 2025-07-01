using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

public class BridgeAcceptanceTest
{
    [SetUp]
    public void SetUp() =>
        NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention = t =>
        {
            if (string.IsNullOrWhiteSpace(t.FullName))
            {
                throw new InvalidOperationException($"The type {nameof(t)} has no fullname to work with.");
            }

            var classAndEndpoint = t.FullName.Split('.').Last();

            var testName = classAndEndpoint.Split('+').First();

            var endpointBuilder = classAndEndpoint.Split('+').Last();

            testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

            testName = testName.Replace("_", "");

            return testName + "." + endpointBuilder;
        };
}