namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Transport;

/// <summary>
/// Bridge transport configured for MassTransit interoperability.
/// Automatically applies MassTransit format adapter to all registered endpoints.
/// </summary>
public class MassTransitBridgeTransport : BridgeTransport
{
    readonly MassTransitFormatAdapter adapter = new();

    /// <summary>
    /// Creates a bridge transport configured for MassTransit interoperability
    /// </summary>
    /// <param name="transportDefinition">The underlying transport definition (e.g., RabbitMQ, Azure Service Bus, Amazon SQS)</param>
    public MassTransitBridgeTransport(TransportDefinition transportDefinition)
        : base(transportDefinition)
    {
    }

    /// <summary>
    /// Discovers MassTransit error queues from a queue discovery provider and
    /// automatically configures them with the MassTransit format adapter
    /// </summary>
    /// <param name="queueDiscovery">The queue discovery provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DiscoverQueues(
        IQueueDiscovery queueDiscovery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queueDiscovery);

        await foreach (var queueName in queueDiscovery.GetQueues(cancellationToken).ConfigureAwait(false))
        {
            HasEndpoint(queueName);
        }
    }

    /// <summary>
    /// Registers a MassTransit endpoint with automatic format adapter configuration
    /// </summary>
    /// <param name="endpointName">The name of the endpoint</param>
    /// <returns>The current transport instance for method chaining</returns>
    public new MassTransitBridgeTransport HasEndpoint(string endpointName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        var endpoint = new BridgeEndpoint(endpointName);
        endpoint.UseMessageFormat(adapter);
        base.HasEndpoint(endpoint);
        return this;
    }

    /// <summary>
    /// Registers a MassTransit endpoint with a specific address and automatic format adapter configuration
    /// </summary>
    /// <param name="endpointName">The name of the endpoint</param>
    /// <param name="endpointAddress">The transport-specific address of the endpoint</param>
    /// <returns>The current transport instance for method chaining</returns>
    public new MassTransitBridgeTransport HasEndpoint(string endpointName, string endpointAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointAddress);

        var endpoint = new BridgeEndpoint(endpointName, endpointAddress);
        endpoint.UseMessageFormat(adapter);
        base.HasEndpoint(endpoint);
        return this;
    }
}
