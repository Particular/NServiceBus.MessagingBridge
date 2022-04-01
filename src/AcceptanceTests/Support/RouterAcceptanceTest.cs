using NServiceBus.Transport;

public class RouterAcceptanceTest
{
    public RouterAcceptanceTest()
    {
        var transportConfig = TestSuiteConfiguration.Current.CreateTransportConfiguration();

        TransportBeingTested = transportConfig.GetTransportDefinition();
    }

    protected TransportDefinition TransportBeingTested;
    protected TransportDefinition TestTransport;
}