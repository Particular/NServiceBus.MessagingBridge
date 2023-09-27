namespace NServiceBus.MessagingBridge.Msmq.DelayedDelivery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Faults;
    using Logging;
    using NServiceBus.Transport;
    using Routing;

    class DelayedDeliveryPump
    {
        public DelayedDeliveryPump(MsmqMessageDispatcher dispatcher,
                                   DueDelayedMessagePoller poller,
                                   IDelayedMessageStore storage,
                                   MessagePump messagePump,
                                   string errorQueue,
                                   int numberOfRetries,
                                   Action<string, Exception, CancellationToken> criticalErrorAction,
                                   TimeSpan timeToWaitForStoreCircuitBreaker,
                                   Dictionary<string, string> faultMetadata,
                                   TransportTransactionMode transportTransactionMode)
        {
            this.dispatcher = dispatcher;
            this.poller = poller;
            this.storage = storage;
            this.numberOfRetries = numberOfRetries;
            this.faultMetadata = faultMetadata;
            pump = messagePump;
            this.errorQueue = errorQueue;

            txOption = transportTransactionMode == TransportTransactionMode.TransactionScope
                ? TransactionScopeOption.Required
                : TransactionScopeOption.RequiresNew;

            storeCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("DelayedDeliveryStore", timeToWaitForStoreCircuitBreaker,
                ex => criticalErrorAction("Failed to store delayed message", ex, CancellationToken.None));
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            await pump.Initialize(PushRuntimeSettings.Default, TimeoutReceived, OnError, cancellationToken).ConfigureAwait(false);
            await pump.StartReceive(cancellationToken).ConfigureAwait(false);
            poller.Start();
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            await pump.StopReceive(cancellationToken).ConfigureAwait(false);
            await poller.Stop(cancellationToken).ConfigureAwait(false);
        }

        async Task TimeoutReceived(MessageContext context, CancellationToken cancellationToken)
        {
            if (!context.Headers.TryGetValue(MsmqUtilities.PropertyHeaderPrefix + MsmqMessageDispatcher.TimeoutDestination, out var destination))
            {
                throw new Exception("This message does not represent a timeout");
            }

            if (!context.Headers.TryGetValue(MsmqUtilities.PropertyHeaderPrefix + MsmqMessageDispatcher.TimeoutAt, out var atString))
            {
                throw new Exception("This message does not represent a timeout");
            }

            var id = context.NativeMessageId; //Use native message ID as a key in the delayed delivery table
            var at = DateTimeOffsetHelper.ToDateTimeOffset(atString);

            var message = context.Extensions.Get<MSMQ.Messaging.Message>();

            var diff = DateTime.UtcNow - at;

            if (diff.Ticks > 0) // Due
            {
                dispatcher.DispatchDelayedMessage(id, message.Extension, context.Body, destination, context.TransportTransaction);
            }
            else
            {
                var timeout = new DelayedMessage
                {
                    Destination = destination,
                    MessageId = id,
                    Body = context.Body.ToArray(),
                    Time = at.UtcDateTime,
                    Headers = message.Extension
                };

                try
                {
                    using (var tx = new TransactionScope(txOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await storage.Store(timeout, cancellationToken).ConfigureAwait(false);
                        tx.Complete();
                    }

                    storeCircuitBreaker.Success();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    //Shutting down
                    return;
                }
                catch (Exception e)
                {
                    await storeCircuitBreaker.Failure(e, cancellationToken).ConfigureAwait(false);
                    throw new Exception("Error while storing delayed message", e);
                }

#pragma warning disable PS0022 // A DateTime should not be implicitly cast to a DateTimeOffset
                poller.Signal(timeout.Time);
#pragma warning restore PS0022 // A DateTime should not be implicitly cast to a DateTimeOffset
            }
        }

        async Task<ErrorHandleResult> OnError(ErrorContext errorContext, CancellationToken cancellationToken)
        {
            Log.Error($"OnError {errorContext.Message.MessageId}", errorContext.Exception);

            if (errorContext.ImmediateProcessingFailures < numberOfRetries)
            {
                return ErrorHandleResult.RetryRequired;
            }

            var message = errorContext.Message;

            ExceptionHeaderHelper.SetExceptionHeaders(message.Headers, errorContext.Exception);
            message.Headers[FaultsHeaderKeys.FailedQ] = errorContext.ReceiveAddress;
            foreach (var pair in faultMetadata)
            {
                message.Headers[pair.Key] = pair.Value;
            }

            var outgoingMessage = new OutgoingMessage(message.NativeMessageId, message.Headers, message.Body);
            var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueue));
            await dispatcher.Dispatch(new TransportOperations(transportOperation), errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            return ErrorHandleResult.Handled;
        }

        readonly MsmqMessageDispatcher dispatcher;
        readonly DueDelayedMessagePoller poller;
        readonly IDelayedMessageStore storage;
        readonly int numberOfRetries;
        readonly MessagePump pump;
        readonly Dictionary<string, string> faultMetadata;
        readonly string errorQueue;
        RepeatedFailuresOverTimeCircuitBreaker storeCircuitBreaker;
        readonly TransactionScopeOption txOption;
        readonly TransactionOptions transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };

        static readonly ILog Log = LogManager.GetLogger<DelayedDeliveryPump>();
    }
}