namespace NServiceBus.Transport.Bridge
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// TBD
    /// </summary>
    public interface IStoppableBridge
    {
        /// <summary>
        /// TBD
        /// </summary>
        Task Stop(CancellationToken cancellationToken = default);
    }
}