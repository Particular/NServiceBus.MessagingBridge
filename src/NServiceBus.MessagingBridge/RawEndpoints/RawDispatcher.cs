namespace NServiceBus.Raw;

using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

class RawDispatcher(TransportInfrastructure transportInfrastructure) : IRawDispatcher
{
    public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default) =>
        transportInfrastructure.Dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);

    public string ToTransportAddress(QueueAddress logicalAddress) =>
        transportInfrastructure.ToTransportAddress(logicalAddress);
}