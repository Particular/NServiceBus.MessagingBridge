namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for a specific bridge endpoint.
    /// </summary>
    public class BridgeEndpoint
    {
        /// <summary>
        /// Initializes an endpoint with the given name.
        /// </summary>
        public BridgeEndpoint(string name) : this(name, name)
        {
        }

        /// <summary>
        /// Intializes an endpoint with the given name and specific transport address.
        /// This overload is needed when using MSMQ and the bridge is running on a separate server.
        /// </summary>
        public BridgeEndpoint(string name, string queueAddress)
        {
            Name = name;
            QueueAddress = queueAddress;

            Subscriptions = new List<Subscription>();
        }

        /// <summary>
        /// Registers the publisher of the event type `T`.
        /// </summary>
        public void RegisterPublisher<T>(string publisher)
        {
            RegisterPublisher(typeof(T), publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type.
        /// </summary>
        public void RegisterPublisher(Type eventType, string publisher)
        {
            RegisterPublisher(eventType.FullName, publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type fullname.
        /// </summary>
        public void RegisterPublisher(string eventTypeFullName, string publisher)
        {
            Subscriptions.Add(new Subscription(eventTypeFullName, publisher));
        }

        internal string Name { get; private set; }

        internal string QueueAddress { get; private set; }

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