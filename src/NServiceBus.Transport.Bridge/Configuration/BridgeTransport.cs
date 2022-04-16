namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// TBD
    /// </summary>
    public class BridgeTransport
    {
        /// <summary>
        /// TBD
        /// </summary>
        public BridgeTransport(TransportDefinition transportDefinition)
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
        public void HasEndpoint(string endpointName)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var endpointAddress = TransportDefinition.ToTransportAddress(new QueueAddress(endpointName));
#pragma warning restore CS0618 // Type or member is obsolete
            HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void HasEndpoint(string endpointName, string endpointAddress)
        {
            HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
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