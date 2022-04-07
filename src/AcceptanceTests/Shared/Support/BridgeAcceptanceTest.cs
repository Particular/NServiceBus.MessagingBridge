using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Transport.Bridge;
using NUnit.Framework;

public class BridgeAcceptanceTest
{
    [SetUp]
    public void SetUp()
    {
        NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention = t =>
        {
            var classAndEndpoint = t.FullName.Split('.').Last();

            var testName = classAndEndpoint.Split('+').First();

            var endpointBuilder = classAndEndpoint.Split('+').Last();

            testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

            testName = testName.Replace("_", "");

            return testName + "." + endpointBuilder;
        };

        var transportConfig = TestSuiteConfiguration.Current.CreateTransportConfiguration();
        BridgeTransportDefinition = transportConfig.GetBridgeTransport();
    }

    [TearDown]
    public Task TearDown()
    {
        return BridgeTransportDefinition.Cleanup(CancellationToken.None);
    }

    protected TransportConfiguration AddTestTransport(BridgeConfiguration routerConfiguration)
    {
        return routerConfiguration.AddTransport(DefaultTestServer.GetTestTransportDefinition(), "right");
    }

    protected TransportDefinition TransportBeingTested => BridgeTransportDefinition.TransportDefinition;
    protected TransportDefinition TestTransport;

    BridgeTransportDefinition BridgeTransportDefinition;
}