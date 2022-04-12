namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// TBD
    /// </summary>
    public class BridgeEndpoint
    {
        /// <summary>
        /// TBD
        /// </summary>
        public BridgeEndpoint(string name) : this(new QueueAddress(name))
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public BridgeEndpoint(QueueAddress queueAddress)
        {
            QueueAddress = queueAddress;
            Name = queueAddress.BaseAddress;

            Subscriptions = new List<BridgeEndpointSubscription>();
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
            Subscriptions.Add(new BridgeEndpointSubscription(eventTypeFullName, publisher));
        }

        /// <summary>
        /// TBD
        /// </summary>
        public QueueAddress QueueAddress { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        public string Name { get; private set; }

        internal List<BridgeEndpointSubscription> Subscriptions { get; set; }
    }
}