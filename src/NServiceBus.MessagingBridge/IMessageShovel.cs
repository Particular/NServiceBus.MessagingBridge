using System.Threading;
using System.Threading.Tasks;

interface IMessageShovel
{
    Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default);
}