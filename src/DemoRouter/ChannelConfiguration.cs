using System.Collections.Generic;
using NServiceBus.Raw;
using NServiceBus.Transport;

public class ChannelConfiguration
{
    public TransportDefinition TransportDefinition { get; private set; }

    public ChannelConfiguration(TransportDefinition transportDefinition)
    {
        Endpoints = new List<QueueAddress>();
        TransportDefinition = transportDefinition;
    }

    public ChannelConfiguration HasEndpoint(string endpoint)
    {
        return HasEndpoint(new QueueAddress(endpoint));
    }

    public ChannelConfiguration HasEndpoint(QueueAddress queueAddress)
    {
        Endpoints.Add(queueAddress);
        return this;
    }

    internal List<QueueAddress> Endpoints { get; private set; }
    internal IReceivingRawEndpoint RunningEndpoint { get; set; }
}