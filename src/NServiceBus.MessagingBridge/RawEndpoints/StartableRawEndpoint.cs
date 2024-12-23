namespace NServiceBus.Raw
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Transport;

    class StartableRawEndpoint : IStartableRawEndpoint
    {
        public StartableRawEndpoint(
            RawEndpointConfiguration rawEndpointConfiguration,
            TransportInfrastructure transportInfrastructure,
            RawCriticalError criticalError)
        {
            this.criticalError = criticalError;
            messagePump = transportInfrastructure.Receivers.Values.FirstOrDefault();
            dispatcher = transportInfrastructure.Dispatcher;
            this.rawEndpointConfiguration = rawEndpointConfiguration;
            this.transportInfrastructure = transportInfrastructure;
            SubscriptionManager = messagePump?.Subscriptions;
            TransportAddress = messagePump?.ReceiveAddress;
        }

        public async Task<IReceivingRawEndpoint> Start(CancellationToken cancellationToken = default)
        {
            if (startHasBeenCalled)
            {
                throw new InvalidOperationException("Multiple calls to Start is not supported.");
            }

            startHasBeenCalled = true;

            RawTransportReceiver receiver = null;

            if (!rawEndpointConfiguration.SendOnly)
            {
                receiver = BuildMainReceiver();

                if (receiver != null)
                {
                    await receiver.Init(cancellationToken).ConfigureAwait(false);
                }
            }

            var runningInstance = new RunningRawEndpointInstance(EndpointName, receiver, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance, cancellationToken);

            try
            {
                if (receiver != null)
                {
                    await receiver.Start(cancellationToken).ConfigureAwait(false);
                }

                return runningInstance;
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                throw;
            }
            catch
            {
                await runningInstance.Stop(cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        public ISubscriptionManager SubscriptionManager { get; }

        public string TransportAddress { get; }
        public string EndpointName => rawEndpointConfiguration.EndpointName;

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default) => dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);

        public string ToTransportAddress(QueueAddress logicalAddress) => transportInfrastructure.ToTransportAddress(logicalAddress);

        RawTransportReceiver BuildMainReceiver()
        {
            var endpointName = rawEndpointConfiguration.EndpointName;

            // the transport seam assumes the error queue address to be a native address so we need to translate
            var errorQueue = transportInfrastructure.ToTransportAddress(new QueueAddress(rawEndpointConfiguration.PoisonMessageQueue));

            var receiver = new RawTransportReceiver(
                messagePump,
                dispatcher,
                rawEndpointConfiguration.OnMessage,
                rawEndpointConfiguration.PushRuntimeSettings,
                new RawEndpointErrorHandlingPolicy(endpointName, errorQueue, dispatcher, rawEndpointConfiguration.ErrorHandlingPolicy));
            return receiver;
        }

        bool startHasBeenCalled;
        readonly RawEndpointConfiguration rawEndpointConfiguration;
        readonly TransportInfrastructure transportInfrastructure;
        readonly RawCriticalError criticalError;
        readonly IMessageReceiver messagePump;
        readonly IMessageDispatcher dispatcher;
    }
}