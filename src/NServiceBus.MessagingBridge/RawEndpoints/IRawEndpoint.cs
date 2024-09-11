namespace NServiceBus.Raw
{
    using Transport;

    /// <summary>
    /// Allows to send raw messages.
    /// </summary>
    interface IRawEndpoint : IRawDispatcher
    {
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