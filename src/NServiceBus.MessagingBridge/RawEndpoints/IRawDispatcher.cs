namespace NServiceBus.Raw
{
    using Transport;

    /// <summary>
    /// Allows to send raw messages.
    /// </summary>
    interface IRawDispatcher : IMessageDispatcher
    {
        /// <summary>
        /// Translates a given logical address into a transport address.
        /// </summary>
        string ToTransportAddress(QueueAddress logicalAddress);
    }
}