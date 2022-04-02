using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        routerTransportDefinition = transportConfig.GetRouterTransport();
    }

    [TearDown]
    public Task TearDown()
    {
        return routerTransportDefinition.Cleanup(CancellationToken.None);
    }

    protected TransportDefinition TransportBeingTested => routerTransportDefinition.TransportDefinition;
    protected TransportDefinition TestTransport;

    RouterTransportDefinition routerTransportDefinition;
}