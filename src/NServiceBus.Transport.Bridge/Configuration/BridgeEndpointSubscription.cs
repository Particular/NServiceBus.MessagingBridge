namespace NServiceBus
{
    public class BridgeEndpointSubscription
    {
        public BridgeEndpointSubscription(string eventTypeFullName, string publisher)
        {
            EventTypeFullName = eventTypeFullName;
            Publisher = publisher;
        }

        public string EventTypeFullName { get; private set; }
        public string Publisher { get; private set; }
    }
}