using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

class TargetEndpointDispatcher
{
    public TargetEndpointDispatcher(string transportName, IRawEndpoint rawEndpoint, string queueAddress)
    {
        this.rawEndpoint = rawEndpoint;
        TransportName = transportName;

        targetAddress = new UnicastAddressTag(queueAddress);
    }

    public string TransportName { get; }

    public string ToTransportAddress(string endpointName)
    {
        return rawEndpoint.ToTransportAddress(new QueueAddress(endpointName));
    }

    public Task Dispatch(
        OutgoingMessage outgoingMessage,
        TransportTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var transportOperation = new TransportOperation(outgoingMessage, targetAddress);
        return rawEndpoint.Dispatch(new TransportOperations(transportOperation), transaction, cancellationToken);
    }

    readonly IRawEndpoint rawEndpoint;
    readonly UnicastAddressTag targetAddress;
}