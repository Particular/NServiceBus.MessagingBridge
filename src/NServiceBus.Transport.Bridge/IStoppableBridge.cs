namespace NServiceBus.Transport.Bridge
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IStoppableBridge
    {
        Task Stop(CancellationToken cancellationToken = default);
    }
}