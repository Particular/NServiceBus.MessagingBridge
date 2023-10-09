namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Particular.Msmq;
    using Performance.TimeToBeReceived;
    using Transport;
    using Unicast.Queuing;

    class MsmqMessageDispatcher : IMessageDispatcher
    {
        readonly MsmqBridgeTransport transportSettings;
        readonly Action<TransportTransaction, UnicastTransportOperation> onSendCallback;

        public MsmqMessageDispatcher(MsmqBridgeTransport transportSettings, Action<TransportTransaction, UnicastTransportOperation> onSendCallback = null)
        {
            this.transportSettings = transportSettings;
            this.onSendCallback = onSendCallback;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(outgoingMessages), outgoingMessages);

            if (outgoingMessages.MulticastTransportOperations.Any())
            {
                throw new Exception("The MSMQ transport only supports unicast transport operations.");
            }

            foreach (var unicastTransportOperation in outgoingMessages.UnicastTransportOperations)
            {
                ExecuteTransportOperation(transaction, unicastTransportOperation);
            }

            return Task.CompletedTask;
        }

        void ExecuteTransportOperation(TransportTransaction transaction, UnicastTransportOperation transportOperation)
        {
            SendToDestination(transaction, transportOperation);
        }

        void SendToDestination(TransportTransaction transaction, UnicastTransportOperation transportOperation)
        {
            onSendCallback?.Invoke(transaction, transportOperation);
            var message = transportOperation.Message;
            var destinationAddress = MsmqAddress.Parse(transportOperation.Destination);

            var dispatchProperties = transportOperation.Properties;

            if (IsCombiningTimeToBeReceivedWithTransactions(
                transaction,
                transportOperation.RequiredDispatchConsistency,
                dispatchProperties))
            {
                if (transportSettings.UseNonNativeTimeToBeReceivedInTransactions)
                {
                    dispatchProperties.DiscardIfNotReceivedBefore =
                        new DiscardIfNotReceivedBefore(Message.InfiniteTimeout);
                }
                else
                {
                    throw new Exception(
                        $"Failed to send message to address: {destinationAddress.Queue}@{destinationAddress.Machine}. Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ.");
                }
            }

            try
            {
                using (var q = new MessageQueue(destinationAddress.FullPath, false, transportSettings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (var toSend = MsmqUtilities.Convert(message))
                    {
                        if (dispatchProperties.DiscardIfNotReceivedBefore?.MaxTime < MessageQueue.InfiniteTimeout)
                        {
                            toSend.TimeToBeReceived = dispatchProperties.DiscardIfNotReceivedBefore.MaxTime;
                        }

                        var useDeadLetterQueue = dispatchProperties.ShouldUseDeadLetterQueue();
                        if (useDeadLetterQueue.HasValue)
                        {
                            toSend.UseDeadLetterQueue = useDeadLetterQueue.Value;
                        }
                        else
                        {
                            var ttbrRequested = toSend.TimeToBeReceived < MessageQueue.InfiniteTimeout;
                            toSend.UseDeadLetterQueue = ttbrRequested
                                ? transportSettings.UseDeadLetterQueueForMessagesWithTimeToBeReceived
                                : transportSettings.UseDeadLetterQueue;
                        }

                        toSend.UseJournalQueue = dispatchProperties.ShouldUseJournalQueue() ??
                                                 transportSettings.UseJournalQueue;

                        toSend.TimeToReachQueue = transportSettings.TimeToReachQueue;

                        if (message.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress))
                        {
                            toSend.ResponseQueue = new MessageQueue(MsmqAddress.Parse(replyToAddress).FullPath);
                        }

                        var label = GetLabel(message);

                        if (transportOperation.RequiredDispatchConsistency == DispatchConsistency.Isolated)
                        {
                            q.Send(toSend, label, GetIsolatedTransactionType());
                            return;
                        }

                        if (TryGetNativeTransaction(transaction, out var activeTransaction))
                        {
                            q.Send(toSend, label, activeTransaction);
                            return;
                        }

                        q.Send(toSend, label, GetTransactionTypeForSend());
                    }
                }
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                {
                    var msg = transportOperation.Destination == null
                        ? "Failed to send message. Target address is null."
                        : $"Failed to send message to address: [{transportOperation.Destination}]";

                    throw new QueueNotFoundException(transportOperation.Destination, msg, ex);
                }

                ThrowFailedToSendException(transportOperation.Destination, ex);
            }
            catch (Exception ex)
            {
                ThrowFailedToSendException(transportOperation.Destination, ex);
            }
        }

        bool IsCombiningTimeToBeReceivedWithTransactions(TransportTransaction transaction,
            DispatchConsistency requiredDispatchConsistency, DispatchProperties dispatchProperties)
        {
            if (!transportSettings.UseTransactionalQueues)
            {
                return false;
            }

            if (requiredDispatchConsistency == DispatchConsistency.Isolated)
            {
                return false;
            }

            var timeToBeReceivedRequested =
                dispatchProperties.DiscardIfNotReceivedBefore?.MaxTime < MessageQueue.InfiniteTimeout;

            if (!timeToBeReceivedRequested)
            {
                return false;
            }

            if (Transaction.Current != null)
            {
                return true;
            }


            return TryGetNativeTransaction(transaction, out _);
        }

        static bool TryGetNativeTransaction(TransportTransaction transportTransaction,
            out MessageQueueTransaction transaction)
        {
            return transportTransaction.TryGet(out transaction);
        }

        MessageQueueTransactionType GetIsolatedTransactionType()
        {
            return transportSettings.UseTransactionalQueues
                ? MessageQueueTransactionType.Single
                : MessageQueueTransactionType.None;
        }

        string GetLabel(OutgoingMessage message)
        {
            var messageLabel =
                transportSettings.ApplyCustomLabelToOutgoingMessages(
                    new ReadOnlyDictionary<string, string>(message.Headers));
            if (messageLabel == null)
            {
                throw new Exception(
                    "MSMQ label convention returned a null. Either return a valid value or a String.Empty to indicate 'no value'.");
            }

            if (messageLabel.Length > 240)
            {
                throw new Exception(
                    "MSMQ label convention returned a value longer than 240 characters. This is not supported.");
            }

            return messageLabel;
        }

        static void ThrowFailedToSendException(string address, Exception ex)
        {
            if (address == null)
            {
                throw new Exception("Failed to send message.", ex);
            }

            throw new Exception($"Failed to send message to address: {address}", ex);
        }

        MessageQueueTransactionType GetTransactionTypeForSend()
        {
            if (!transportSettings.UseTransactionalQueues)
            {
                return MessageQueueTransactionType.None;
            }

            return Transaction.Current != null
                ? MessageQueueTransactionType.Automatic
                : MessageQueueTransactionType.Single;
        }
    }
}