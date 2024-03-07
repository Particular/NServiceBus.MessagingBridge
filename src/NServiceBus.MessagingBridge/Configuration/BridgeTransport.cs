﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Configuration options for a specific transport in the bridge
    /// </summary>
    public class BridgeTransport
    {
        /// <summary>
        /// Initializes a transport in the bridge with the given transport definition
        /// </summary>
        public BridgeTransport(TransportDefinition transportDefinition)
        {
            Guard.AgainstNull(nameof(transportDefinition), transportDefinition);

            Endpoints = new List<BridgeEndpoint>();
            TransportDefinition = transportDefinition;
            Name = transportDefinition.GetType().Name.ToLower().Replace("transport", "");
            ErrorQueue = "bridge.error";
            AutoCreateQueues = false;
            Concurrency = Math.Max(2, Environment.ProcessorCount);
            Heartbeats = new HeartbeatConfiguration();
            CustomChecks = new CustomChecksConfiguration();
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
        /// Set to true to automatically create the queues necessary for the bridge operation
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
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            HasEndpoint(new BridgeEndpoint(endpointName));
        }

        /// <summary>
        /// Registers an endpoint with the given name and transport address with the current transport
        /// </summary>
        public void HasEndpoint(string endpointName, string endpointAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);
            Guard.AgainstNullAndEmpty(nameof(endpointAddress), endpointAddress);

            HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
        }

        /// <summary>
        ///  Registers the given endpoint with the current transport
        /// </summary>
        public void HasEndpoint(BridgeEndpoint endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);

            Endpoints.Add(endpoint);
        }

        /// <summary>
        /// Sets the ServiceControl queue address.
        /// </summary>
        /// <param name="serviceControlQueue"></param>
        /// <param name="frequency"></param>
        /// <param name="timeToLive"></param>
        public void SendHeartbeatTo(string serviceControlQueue, TimeSpan? frequency = null, TimeSpan? timeToLive = null)
        {
            var freq = frequency ?? TimeSpan.FromSeconds(10);
            var ttl = timeToLive ?? TimeSpan.FromTicks(freq.Ticks * 4);
            Heartbeats.ServiceControlQueue = serviceControlQueue;
            Heartbeats.Frequency = freq;
            Heartbeats.TimeToLive = ttl;
        }

        /// <summary>
        /// Sets the ServiceControl queue address.
        /// </summary>
        /// <param name="serviceControlQueue">ServiceControl queue address.</param>
        /// <param name="timeToLive">The maximum time to live for the custom check report messages. Defaults to 4 times the check interval.</param>
        public void ReportCustomChecksTo(string serviceControlQueue, TimeSpan? timeToLive = null)
        {
            if (string.IsNullOrWhiteSpace(serviceControlQueue))
            {
                throw new ArgumentNullException(nameof(serviceControlQueue));
            }

            CustomChecks.ServiceControlQueue = serviceControlQueue;

            CustomChecks.TimeToLive = timeToLive;
        }


        internal HeartbeatConfiguration Heartbeats { get; }

        internal CustomChecksConfiguration CustomChecks { get; }

        internal List<BridgeEndpoint> Endpoints { get; }

        internal TransportDefinition TransportDefinition { get; private set; }
    }
}