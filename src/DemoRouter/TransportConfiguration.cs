using System;
using System.Collections.Generic;
using NServiceBus.Raw;
using NServiceBus.Transport;

public class TransportConfiguration
{
    public TransportDefinition TransportDefinition { get; private set; }

    public TransportConfiguration(TransportDefinition transportDefinition)
    {
        Endpoints = new List<Endpoint>();
        TransportDefinition = transportDefinition;
    }

    public TransportConfiguration HasEndpoint(string endpoint)
    {
        return HasEndpoint(new QueueAddress(endpoint));
    }

    public TransportConfiguration HasEndpoint(QueueAddress queueAddress)
    {
        ConfiguringEndpoint = new Endpoint { QueueAddress = queueAddress };
        ConfiguringEndpoint.Subsriptions = new List<Subscription>();

        Endpoints.Add(ConfiguringEndpoint);
        return this;
    }

    internal List<Endpoint> Endpoints { get; private set; }
    internal IReceivingRawEndpoint RunningEndpoint { get; set; }
    Endpoint ConfiguringEndpoint { get; set; }

    public TransportConfiguration RegisterPublisher(Type eventType, string publisher)
    {
        ConfiguringEndpoint.Subsriptions.Add(new Subscription()
        {
            EventType = eventType,
            EventTypeFullName = eventType.FullName,
            Publisher = publisher
        });

        return this;
    }

    public TransportConfiguration RegisterPublisher(string eventTypeFullName, string publisher)
    {
        ConfiguringEndpoint.Subsriptions.Add(new Subscription()
        {
            EventTypeFullName = eventTypeFullName,
            Publisher = publisher
        });

        return this;
    }
}

class Endpoint
{
    public QueueAddress QueueAddress { get; set; }
    public List<Subscription> Subsriptions { get; set; }
}

class Subscription
{
    public Type EventType { get; set; }
    public string EventTypeFullName { get; set; }
    public string Publisher { get; set; }
}