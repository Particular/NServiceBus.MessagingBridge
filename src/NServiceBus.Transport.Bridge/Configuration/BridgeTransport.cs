namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Configuration options for a specific transport in the transport bridge
    /// </summary>
    public class BridgeTransport
    {
        /// <summary>
        /// Initializes a transport in the bridge with the given transport definition
        /// </summary>
        public BridgeTransport(TransportDefinition transportDefinition)
        {
            Endpoints = new List<BridgeEndpoint>();
            TransportDefinition = transportDefinition;
            Name = transportDefinition.GetType().Name.ToLower().Replace("transport", "");
            ErrorQueue = "bridge.error";
            AutoCreateQueues = false;
            Concurrency = Math.Max(2, Environment.ProcessorCount);
        }

        /// <summary>
        /// Overrides the default name of the transport. Used when multiple transports of the same type are used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Specifies the name of a custom error queue
        /// </summary>
        public string ErrorQueue { get; set; }

        /// <summary>
        /// Set to true to automatically create the queues necessary for transport bridge operation
        /// </summary>
        public bool AutoCreateQueues { get; set; }

        /// <summary>
        /// Configures the concurrency used to move messages from the current transport to bridged transports
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// Registers an endpoint with the given name with the current transport
        /// </summary>
        public void HasEndpoint(string endpointName)
        {
            HasEndpoint(new BridgeEndpoint(endpointName));
        }

        /// <summary>
        /// Registers an endpoint with the given name and transport address with the current transport
        /// </summary>
        public void HasEndpoint(string endpointName, string endpointAddress)
        {
            HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
        }

        /// <summary>
        ///  Registers the given endpoint with the current transport
        /// </summary>
        public void HasEndpoint(BridgeEndpoint endpoint)
        {
            Endpoints.Add(endpoint);
        }

        internal TransportDefinition TransportDefinition { get; private set; }

        internal List<BridgeEndpoint> Endpoints { get; private set; }
    }
}