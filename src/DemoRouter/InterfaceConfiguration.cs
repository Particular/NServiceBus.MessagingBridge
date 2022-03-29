using System.Collections.Generic;
using NServiceBus.Raw;
using NServiceBus.Transport;

public class InterfaceConfiguration
{
    public TransportDefinition TransportDefinition { get; private set; }

    public InterfaceConfiguration(TransportDefinition transportDefinition)
    {
        Endpoints = new List<string>();
        TransportDefinition = transportDefinition;
    }

    public InterfaceConfiguration HasEndpoint(string endpoint)
    {
        Endpoints.Add(endpoint);
        return this;
    }

    internal List<string> Endpoints { get; private set; }
    internal IReceivingRawEndpoint RunningEndpoint { get; set; }
}