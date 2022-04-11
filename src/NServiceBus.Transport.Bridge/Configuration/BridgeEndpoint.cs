namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    public class BridgeEndpoint
    {
        public BridgeEndpoint(string name) : this(new QueueAddress(name))
        {
        }

        public BridgeEndpoint(QueueAddress queueAddress)
        {
            QueueAddress = queueAddress;
            Name = queueAddress.BaseAddress;

            Subscriptions = new List<BridgeEndpointSubscription>();
        }

        public void RegisterPublisher<T>(string publisher)
        {
            RegisterPublisher(typeof(T), publisher);
        }

        public void RegisterPublisher(Type eventType, string publisher)
        {
            RegisterPublisher(eventType.FullName, publisher);
        }

        public void RegisterPublisher(string eventTypeFullName, string publisher)
        {
            Subscriptions.Add(new BridgeEndpointSubscription(eventTypeFullName, publisher));
        }

        public QueueAddress QueueAddress { get; private set; }
        public string Name { get; private set; }

        internal List<BridgeEndpointSubscription> Subscriptions { get; set; }
    }
}