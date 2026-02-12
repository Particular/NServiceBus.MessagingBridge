using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Performance.TimeToBeReceived;
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

    public Task Dispatch(
        OutgoingMessage outgoingMessage,
        TransportTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var transportOperation = new TransportOperation(outgoingMessage, targetAddress);

        if (outgoingMessage.Headers.TryGetValue(Headers.TimeToBeReceived, out var timeToBeReceivedValue)
            && TimeSpan.TryParse(timeToBeReceivedValue, out var timeToBeReceived))
        {
            transportOperation.Properties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived);
        }

        return rawEndpoint.Dispatch(new TransportOperations(transportOperation), transaction, cancellationToken);
    }

    readonly IRawEndpoint rawEndpoint;
    readonly UnicastAddressTag targetAddress;
}