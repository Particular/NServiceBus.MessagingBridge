namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Configuration for a specific bridge transport.
    /// </summary>
    public class BridgeTransport
    {
        /// <summary>
        /// Initializes an transport with the given transport definition.
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
        /// Overrides the default name. Used when multiple transports of the same type is used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Configures a custom error queue.
        /// </summary>
        public string ErrorQueue { get; set; }

        /// <summary>
        /// Configures automatic queue creation.
        /// </summary>
        public bool AutoCreateQueues { get; set; }

        /// <summary>
        /// Configures the concurrency used to move messages from this transport across to other transports.
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// Registers the endpoint with the given name as connected to this transport.
        /// </summary>
        public void HasEndpoint(string endpointName)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var endpointAddress = TransportDefinition.ToTransportAddress(new QueueAddress(endpointName));
#pragma warning restore CS0618 // Type or member is obsolete
            HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
        }

        /// <summary>
        /// Registers the endpoint with the given name and transport address as connected to this transport.
        /// </summary>
        public void HasEndpoint(string endpointName, string endpointAddress)
        {
            HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
        }

        /// <summary>
        ///  Registers the given endpoint with its transport address as connected to this transport.
        /// </summary>
        public void HasEndpoint(BridgeEndpoint endpoint)
        {
            Endpoints.Add(endpoint);
        }

        internal TransportDefinition TransportDefinition { get; private set; }

        internal List<BridgeEndpoint> Endpoints { get; private set; }
    }
}