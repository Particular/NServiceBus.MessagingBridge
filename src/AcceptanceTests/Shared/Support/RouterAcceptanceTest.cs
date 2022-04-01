using System.Linq;
using System.Threading;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.Transport;
using NUnit.Framework;

public class RouterAcceptanceTest
{
    [SetUp]
    public void SetUp()
    {
        Conventions.EndpointNamingConvention = t =>
        {
            var classAndEndpoint = t.FullName.Split('.').Last();

            var testName = classAndEndpoint.Split('+').First();

            var endpointBuilder = classAndEndpoint.Split('+').Last();

            testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

            testName = testName.Replace("_", "");

            return testName + "." + endpointBuilder;
        };

        var transportConfig = TestSuiteConfiguration.Current.CreateTransportConfiguration();

        TransportBeingTested = transportConfig.GetTransportDefinition();
    }


    protected TransportDefinition TransportBeingTested;
    protected TransportDefinition TestTransport;
}