namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    class RunningRawEndpointInstance : IReceivingRawEndpoint
    {
        public RunningRawEndpointInstance(
            string endpointName,
            RawTransportReceiver receiver,
            TransportInfrastructure transportInfrastructure)
        {
            this.receiver = receiver;
            this.transportInfrastructure = transportInfrastructure;
            EndpointName = endpointName;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            return transportInfrastructure.Dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }

        public string ToTransportAddress(QueueAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        public async Task<IStoppableRawEndpoint> StopReceiving(CancellationToken cancellationToken = default)
        {
            if (receiver != null)
            {
                Log.Info("Stopping receiver.");
                await receiver.Stop(cancellationToken).ConfigureAwait(false);
                Log.Info("Receiver stopped.");
            }
            return new StoppableRawEndpoint(transportInfrastructure);
        }

        public string TransportAddress => receiver.Receiver.ReceiveAddress;
        public string EndpointName { get; }
        public ISubscriptionManager SubscriptionManager => receiver.Receiver.Subscriptions;

        public IReadOnlySettings Settings => throw new System.NotImplementedException();

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            var stoppable = await StopReceiving(cancellationToken).ConfigureAwait(false);
            await stoppable.Stop(cancellationToken).ConfigureAwait(false);
        }

        TransportInfrastructure transportInfrastructure;
        RawTransportReceiver receiver;

        static ILog Log = LogManager.GetLogger<RunningRawEndpointInstance>();
    }
}