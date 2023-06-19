using System.Threading;
using System.Threading.Tasks;

interface IStoppableBridge
{
    Task Stop(CancellationToken cancellationToken = default);
}