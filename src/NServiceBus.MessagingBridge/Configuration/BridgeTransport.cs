namespace NServiceBus;

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
        ArgumentNullException.ThrowIfNull(transportDefinition);

        Endpoints = [];
        TransportDefinition = transportDefinition;
        Name = transportDefinition.GetType().Name.ToLower().Replace("transport", "");
        ErrorQueue = "bridge.error";
        AutoCreateQueues = false;
        Concurrency = Math.Max(2, Environment.ProcessorCount);
        Heartbeats = new();
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
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        HasEndpoint(new BridgeEndpoint(endpointName));
    }

    /// <summary>
    /// Registers an endpoint with the given name and transport address with the current transport
    /// </summary>
    public void HasEndpoint(string endpointName, string endpointAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointAddress);

        HasEndpoint(new BridgeEndpoint(endpointName, endpointAddress));
    }

    /// <summary>
    ///  Registers the given endpoint with the current transport
    /// </summary>
    public void HasEndpoint(BridgeEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        Endpoints.Add(endpoint);
    }

    /// <summary>
    /// Sets the ServiceControl queue address.
    /// </summary>
    /// <param name="serviceControlQueue">ServiceControl queue address.</param>
    /// <param name="frequency">The frequency to send heartbeats.</param>
    /// <param name="timeToLive">The maximum time to live for the heartbeat.</param>
    public void SendHeartbeatTo(string serviceControlQueue, TimeSpan? frequency = null, TimeSpan? timeToLive = null)
    {
        var freq = frequency ?? TimeSpan.FromSeconds(10);
        var ttl = timeToLive ?? TimeSpan.FromTicks(freq.Ticks * 4);
        Heartbeats.ServiceControlQueue = serviceControlQueue;
        Heartbeats.Frequency = freq;
        Heartbeats.TimeToLive = ttl;
    }

    internal HeartbeatConfiguration Heartbeats { get; }

    internal TransportDefinition TransportDefinition { get; private set; }

    internal List<BridgeEndpoint> Endpoints { get; }
}