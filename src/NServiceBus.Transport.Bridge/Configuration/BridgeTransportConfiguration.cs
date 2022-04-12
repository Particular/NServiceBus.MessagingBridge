namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// TBD
    /// </summary>
    public class BridgeTransportConfiguration
    {
        /// <summary>
        /// TBD
        /// </summary>
        public BridgeTransportConfiguration(TransportDefinition transportDefinition)
        {
            Endpoints = new List<BridgeEndpoint>();
            TransportDefinition = transportDefinition;
            Name = transportDefinition.GetType().Name.ToLower().Replace("transport", "");
            ErrorQueue = "bridge.error";
            AutoCreateQueues = true;
            Concurrency = 1;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public TransportDefinition TransportDefinition { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// TBD
        /// </summary>
        public string ErrorQueue { get; set; }

        /// <summary>
        /// TBD
        /// </summary>
        public bool AutoCreateQueues { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// TBD
        /// </summary>
        public void HasEndpoint(string endpoint)
        {
            HasEndpoint(new QueueAddress(endpoint));
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void HasEndpoint(QueueAddress queueAddress)
        {
            var endpoint = new BridgeEndpoint(queueAddress);

            HasEndpoint(endpoint);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void HasEndpoint(BridgeEndpoint endpoint)
        {
            Endpoints.Add(endpoint);
        }

        internal List<BridgeEndpoint> Endpoints { get; private set; }
    }
}