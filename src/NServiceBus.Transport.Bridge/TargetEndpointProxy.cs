using NServiceBus.Raw;

class TargetEndpointProxy
{
    public TargetEndpointProxy(string transportName, IRawEndpoint rawEndpoint)
    {
        TransportName = transportName;
        RawEndpoint = rawEndpoint;
    }

    public IRawEndpoint RawEndpoint { get; }
    public string TransportName { get; }
}