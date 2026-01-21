namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Adapter for transforming messages between foreign message formats and NServiceBus format
/// </summary>
public interface IMessageFormatAdapter
{
    /// <summary>
    /// Name of the message format (e.g., "MassTransit", "Wolverine", "Rebus")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Transform a message from the foreign format to NServiceBus format.
    /// Called when receiving messages from foreign systems.
    /// </summary>
    Task TransformIncoming(Transport.MessageContext messageContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transform a message from NServiceBus format to the foreign format.
    /// Called when sending messages to foreign systems.
    /// </summary>
    Task TransformOutgoing(Transport.MessageContext messageContext, string targetTransportName, CancellationToken cancellationToken = default);
}
