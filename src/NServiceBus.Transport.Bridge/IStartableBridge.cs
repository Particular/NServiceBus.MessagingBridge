namespace NServiceBus.Transport.Bridge
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IStartableBridge
    {
        Task<IStoppableBridge> Start(CancellationToken cancellationToken = default);
    }
}