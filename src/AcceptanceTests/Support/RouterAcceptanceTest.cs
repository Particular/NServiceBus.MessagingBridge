using NServiceBus.Transport;
using NUnit.Framework;

public class RouterAcceptanceTest
{
    [SetUp]
    public void SetUp()
    {
        var transportConfig = TestSuiteConfiguration.Current.CreateTransportConfiguration();

        TransportBeingTested = transportConfig.GetTransportDefinition();
    }


    protected TransportDefinition TransportBeingTested;
    protected TransportDefinition TestTransport;
}