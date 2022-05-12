namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for a specific endpoint in the transport bridge
    /// </summary>
    public class BridgeEndpoint
    {
        /// <summary>
        /// Initializes an endpoint in the transport bridge with the given name
        /// </summary>
        public BridgeEndpoint(string name) : this(name, null)
        {
        }

        /// <summary>
        /// Initializes an endpoint in the transport bridge with the given name and a specific transport address.
        /// This overload is needed when using an MSMQ endpoint and the bridge is running on a separate server.
        /// </summary>
        public BridgeEndpoint(string name, string queueAddress)
        {
            Name = name;
            QueueAddress = queueAddress;

            Subscriptions = new List<Subscription>();
        }

        /// <summary>
        /// Registers the publisher of the event type `T`
        /// </summary>
        public void RegisterPublisher<T>(string publisher)
        {
            RegisterPublisher(typeof(T), publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type
        /// </summary>
        public void RegisterPublisher(Type eventType, string publisher)
        {
            RegisterPublisher(eventType.FullName, publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type using its fully-qualified type name
        /// </summary>
        public void RegisterPublisher(string eventTypeFullName, string publisher)
        {
            Subscriptions.Add(new Subscription(eventTypeFullName, publisher));
        }

        internal string Name { get; private set; }

        internal string QueueAddress { get; set; }

        internal List<Subscription> Subscriptions { get; set; }

        internal class Subscription
        {
            public Subscription(string eventTypeFullName, string publisher)
            {
                EventTypeFullName = eventTypeFullName;
                Publisher = publisher;
            }

            public string EventTypeFullName { get; private set; }

            public string Publisher { get; private set; }
        }
    }
}