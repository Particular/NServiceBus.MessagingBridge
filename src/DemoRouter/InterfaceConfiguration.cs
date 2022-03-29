using NServiceBus.Transport;

public class InterfaceConfiguration
{
    public string Endpoint { get; private set; }

    public TransportDefinition TransportDefinition { get; private set; }

    public InterfaceConfiguration(TransportDefinition transportDefinition) => TransportDefinition = transportDefinition;

    public void HasEndpoint(string endpoint)
    {
        Endpoint = endpoint;
    }
}