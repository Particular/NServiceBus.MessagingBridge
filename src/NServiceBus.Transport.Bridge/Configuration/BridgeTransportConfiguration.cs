namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Transport;

    public class BridgeTransportConfiguration
    {
        public TransportDefinition TransportDefinition { get; private set; }

        public string Name { get; set; }
        public string ErrorQueue { get; set; }
        public bool AutoCreateQueues { get; set; }
        public int Concurrency { get; set; }

        public BridgeTransportConfiguration(TransportDefinition transportDefinition)
        {
            Endpoints = new List<BridgeEndpoint>();
            TransportDefinition = transportDefinition;
            Name = transportDefinition.GetType().Name.ToLower().Replace("transport", "");
            ErrorQueue = "bridge.error";
            AutoCreateQueues = true;
            Concurrency = 1;
        }

        public void HasEndpoint(string endpoint)
        {
            HasEndpoint(new QueueAddress(endpoint));
        }

        public void HasEndpoint(QueueAddress queueAddress)
        {
            var endpoint = new BridgeEndpoint(queueAddress);

            HasEndpoint(endpoint);
        }

        public void HasEndpoint(BridgeEndpoint endpoint)
        {
            Endpoints.Add(endpoint);
        }

        internal List<BridgeEndpoint> Endpoints { get; private set; }
    }
}