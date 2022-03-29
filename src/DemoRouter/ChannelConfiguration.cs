using System.Collections.Generic;
using NServiceBus.Raw;
using NServiceBus.Transport;

public class ChannelConfiguration
{
    public TransportDefinition TransportDefinition { get; private set; }

    public ChannelConfiguration(TransportDefinition transportDefinition)
    {
        Endpoints = new List<string>();
        TransportDefinition = transportDefinition;
    }

    public ChannelConfiguration HasEndpoint(string endpoint)
    {
        Endpoints.Add(endpoint);
        return this;
    }

    internal List<string> Endpoints { get; private set; }
    internal IReceivingRawEndpoint RunningEndpoint { get; set; }
}