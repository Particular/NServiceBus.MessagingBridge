namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IReceivingRawEndpoint : IStoppableRawEndpoint, IRawEndpoint
    {
        /// <summary>
        /// Stops receiving of messages. The endpoint can still send messages.
        /// </summary>
        Task<IStoppableRawEndpoint> StopReceiving(CancellationToken cancellationToken = default);
    }
}