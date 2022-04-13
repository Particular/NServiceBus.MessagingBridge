using System.Threading;
using System.Threading.Tasks;

interface IStartableBridge
{
    Task<IStoppableBridge> Start(CancellationToken cancellationToken = default);
}