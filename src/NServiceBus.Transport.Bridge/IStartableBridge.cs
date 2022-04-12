namespace NServiceBus.Transport.Bridge
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// TBD
    /// </summary>
    public interface IStartableBridge
    {
        /// <summary>
        /// TBD
        /// </summary>
        Task<IStoppableBridge> Start(CancellationToken cancellationToken = default);
    }
}