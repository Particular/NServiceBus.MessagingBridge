namespace NServiceBus.Raw
{
    using Transport;

    /// <summary>
    /// Allows to send raw messages.
    /// </summary>
    public interface IRawEndpoint : IMessageDispatcher
    {
        /// <summary>
        /// Translates a given logical address into a transport address.
        /// </summary>
        string ToTransportAddress(QueueAddress logicalAddress);

        /// <summary>
        /// Returns the transport address of the endpoint.
        /// </summary>
        string TransportAddress { get; }

        /// <summary>
        /// Returns the logical name of the endpoint.
        /// </summary>
        string EndpointName { get; }

        /// <summary>
        /// Gets the subscription manager if the underlying transport supports native publish-subscribe.
        /// </summary>
        ISubscriptionManager SubscriptionManager { get; }
    }
}