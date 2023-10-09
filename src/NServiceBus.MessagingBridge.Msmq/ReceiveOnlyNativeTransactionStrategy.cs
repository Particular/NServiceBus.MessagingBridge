namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using Particular.Msmq;
    using Transport;

    class ReceiveOnlyNativeTransactionStrategy : ReceiveStrategy
    {
        public ReceiveOnlyNativeTransactionStrategy(MsmqFailureInfoStorage failureInfoStorage)
        {
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task ReceiveMessage(CancellationToken cancellationToken = default)
        {
            Message message = null;
            var context = new ContextBag();

            try
            {
                using (var msmqTransaction = new MessageQueueTransaction())
                {
                    msmqTransaction.Begin();

                    if (!TryReceive(msmqTransaction, out message))
                    {
                        return;
                    }
                    context.Set(message);

                    if (!TryExtractHeaders(message, out var headers))
                    {
                        MovePoisonMessageToErrorQueue(message, IsQueuesTransactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);

                        msmqTransaction.Commit();
                        return;
                    }

                    var shouldCommit = await ProcessMessage(message, headers, context, cancellationToken).ConfigureAwait(false);

                    if (shouldCommit)
                    {
                        msmqTransaction.Commit();
                        failureInfoStorage.ClearFailureInfoForMessage(message.Id);
                    }
                    else
                    {
                        msmqTransaction.Abort();
                    }
                }
            }
            // We'll only get here if Commit/Abort/Dispose throws which should be rare.
            // Note: If that happens the attempts counter will be inconsistent since the message might be picked up again before we can register the failure in the LRU cache.
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                if (message == null)
                {
                    throw;
                }

                failureInfoStorage.RecordFailureInfoForMessage(message.Id, ex, context);
            }
        }

        async Task<bool> ProcessMessage(Message message, Dictionary<string, string> headers, ContextBag context, CancellationToken cancellationToken)
        {
            var length = (int)message.BodyStream.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                _ = await message.BodyStream.ReadAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
                var body = buffer.AsMemory(0, length);

                if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out var failureInfo))
                {
                    var errorHandleResult = await HandleError(message, body, failureInfo.Exception, transportTransaction, failureInfo.NumberOfProcessingAttempts, failureInfo.Context, cancellationToken).ConfigureAwait(false);

                    if (errorHandleResult == ErrorHandleResult.Handled)
                    {
                        return true;
                    }
                }

                try
                {
                    await TryProcessMessage(message.Id, headers, body, transportTransaction, context, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    failureInfoStorage.RecordFailureInfoForMessage(message.Id, ex, context);

                    return false;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        MsmqFailureInfoStorage failureInfoStorage;
        static TransportTransaction transportTransaction = new TransportTransaction();
    }
}
