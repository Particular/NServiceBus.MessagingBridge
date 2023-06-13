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
        public BridgeEndpoint(string name)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);

            Name = name;
        }

        /// <summary>
        /// Initializes an endpoint in the transport bridge with the given name and a specific transport address.
        /// This overload is needed when using an MSMQ endpoint and the bridge is running on a separate server.
        /// </summary>
        public BridgeEndpoint(string name, string queueAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Guard.AgainstNullAndEmpty(nameof(queueAddress), queueAddress);

            Name = name;
            QueueAddress = queueAddress;
        }

        /// <summary>
        /// Registers the publisher of the event type `T`
        /// </summary>
        public void RegisterPublisher<T>(string publisher)
        {
            Guard.AgainstNullAndEmpty(nameof(publisher), publisher);

            RegisterPublisher(typeof(T), publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type
        /// </summary>
        public void RegisterPublisher(Type eventType, string publisher)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNullAndEmpty(nameof(publisher), publisher);

            RegisterPublisher(eventType.AssemblyQualifiedName, publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type using its assembly fully-qualified name
        /// </summary>
        public void RegisterPublisher(string eventTypeFullName, string publisher)
        {
            Guard.AgainstNullAndEmpty(nameof(eventTypeFullName), eventTypeFullName);
            Guard.AgainstNullAndEmpty(nameof(publisher), publisher);

            Subscriptions.Add(new Subscription(eventTypeFullName, publisher));
        }

        internal string Name { get; private set; }

        internal string QueueAddress { get; set; }

        internal IList<Subscription> Subscriptions { get; } = new List<Subscription>();

        internal class Subscription
        {
            public Subscription(string eventTypeAssemblyQualifiedName, string publisher)
            {
                EventTypeAssemblyQualifiedName = eventTypeAssemblyQualifiedName;
                Publisher = publisher;
            }

            public string EventTypeAssemblyQualifiedName { get; private set; }

            public string Publisher { get; private set; }
        }
    }
}