namespace NServiceBus
{
    /// <summary>
    /// TBD
    /// </summary>
    public class BridgeEndpointSubscription
    {
        /// <summary>
        /// TBD
        /// </summary>
        public BridgeEndpointSubscription(string eventTypeFullName, string publisher)
        {
            EventTypeFullName = eventTypeFullName;
            Publisher = publisher;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public string EventTypeFullName { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        public string Publisher { get; private set; }
    }
}