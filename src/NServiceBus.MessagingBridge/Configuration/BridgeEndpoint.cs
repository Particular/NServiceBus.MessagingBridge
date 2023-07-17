namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for a specific endpoint in the bridge
    /// </summary>
    public class BridgeEndpoint
    {
        /// <summary>
        /// Initializes an endpoint in the bridge with the given name
        /// </summary>
        public BridgeEndpoint(string name)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);

            Name = name;
        }

        /// <summary>
        /// Initializes an endpoint in the bridge with the given name and a specific transport address.
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

            var fullyQualifiedAssemblyTypeName = eventType.AssemblyQualifiedName;

            const string NeutralSuffix = ", Culture=neutral, PublicKeyToken=null";

            if (fullyQualifiedAssemblyTypeName.EndsWith(NeutralSuffix))
            {
                fullyQualifiedAssemblyTypeName = fullyQualifiedAssemblyTypeName.Substring(0, fullyQualifiedAssemblyTypeName.Length - NeutralSuffix.Length);
            }

            RegisterPublisher(fullyQualifiedAssemblyTypeName, publisher);
        }

        /// <summary>
        /// Registers the publisher of the given event type using its assembly fully-qualified name i.e `MyNamespace.EventName, AssemblyName, Version=1.0.0.0` (the culture and public keys are ignored by NServiceBus)
        /// </summary>
        public void RegisterPublisher(string eventTypeAssemblyQualifiedName, string publisher)
        {
            Guard.AgainstNullAndEmpty(nameof(eventTypeAssemblyQualifiedName), eventTypeAssemblyQualifiedName);
            Guard.AgainstNullAndEmpty(nameof(publisher), publisher);

            try
            {
                // Try retrieving type, this will validate the assembly qualified name. A value cannot
                // be parsed it will throw. If it can be parsed it doesn't mean the type can be
                // resolved thus the result is ignored.
                _ = Type.GetType(eventTypeAssemblyQualifiedName, false);
                Subscriptions.Add(new Subscription(eventTypeAssemblyQualifiedName, publisher));
            }
            catch
            {
                throw new ArgumentException("The event type assembly qualified name is invalid", eventTypeAssemblyQualifiedName);
            }
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

        /// <summary>
        /// Instructs the bridge to not bridge TO this endpoint and not create receive proxies on all other transports but only to allow sending FROM this endpoint to another transport endpoint.
        /// </summary>
        public bool OneWay
        {
            get;
            set;
        }
    }
}