namespace NServiceBus.Raw
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Transport;

    class RawTransportReceiver
    {
        public RawTransportReceiver(
            IMessageReceiver pushMessages,
            IMessageDispatcher dispatcher,
            Func<MessageContext, IMessageDispatcher, CancellationToken, Task> onMessage,
            PushRuntimeSettings pushRuntimeSettings,
            RawEndpointErrorHandlingPolicy errorHandlingPolicy)
        {
            this.errorHandlingPolicy = errorHandlingPolicy;
            this.pushRuntimeSettings = pushRuntimeSettings;
            Receiver = pushMessages;
            this.onMessage = (ctx, ct) => onMessage(ctx, dispatcher, ct);
        }

        public IMessageReceiver Receiver;

        public Task Init(CancellationToken cancellationToken = default)
        {
            return Receiver.Initialize(pushRuntimeSettings, (ctx, ct) => onMessage(ctx, ct), (ctx, ct) => errorHandlingPolicy.OnError(ctx, ct), cancellationToken);
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            try
            {
                Logger.DebugFormat("Receiver is starting, listening to queue {0}.", Receiver.ReceiveAddress);

                await Receiver.StartReceive(cancellationToken).ConfigureAwait(false);

                isStarted = true;
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                throw;
            }
            catch
            {
                isStarted = false;
                await Receiver.StopReceive(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            if (!isStarted)
            {
                return;
            }

            await Receiver.StopReceive(cancellationToken).ConfigureAwait(false);
            if (Receiver is IDisposable disposable)
            {
                disposable.Dispose();
            }
            isStarted = false;
        }

        RawEndpointErrorHandlingPolicy errorHandlingPolicy;
        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        readonly Func<MessageContext, CancellationToken, Task> onMessage;
        static ILog Logger = LogManager.GetLogger<RawTransportReceiver>();
    }
}