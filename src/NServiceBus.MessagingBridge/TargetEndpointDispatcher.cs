using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

class TargetEndpointDispatcher(string transportName, IRawDispatcher rawDispatcher, string queueAddress)
{
    public string TransportName { get; } = transportName;

    public Task Dispatch(
        OutgoingMessage outgoingMessage,
        TransportTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var transportOperation = new TransportOperation(outgoingMessage, targetAddress);
        return rawDispatcher.Dispatch(new TransportOperations(transportOperation), transaction, cancellationToken);
    }

    readonly UnicastAddressTag targetAddress = new(queueAddress);
}