namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// TBD
    /// </summary>
    public class BridgeEndpoint
    {
        /// <summary>
        /// TBD
        /// </summary>
        public BridgeEndpoint(string name) : this(name, name)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public BridgeEndpoint(string name, string queueAddress)
        {
            Name = name;
            QueueAddress = queueAddress;

            Subscriptions = new List<Subscription>();
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void RegisterPublisher<T>(string publisher)
        {
            RegisterPublisher(typeof(T), publisher);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void RegisterPublisher(Type eventType, string publisher)
        {
            RegisterPublisher(eventType.FullName, publisher);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void RegisterPublisher(string eventTypeFullName, string publisher)
        {
            Subscriptions.Add(new Subscription(eventTypeFullName, publisher));
        }

        /// <summary>
        /// TBD
        /// </summary>
        public string QueueAddress { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        public string Name { get; private set; }

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