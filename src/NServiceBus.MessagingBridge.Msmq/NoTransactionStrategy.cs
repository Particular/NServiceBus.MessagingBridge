namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Buffers;
    using MSMQ.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using Transport;

    class NoTransactionStrategy : ReceiveStrategy
    {
        public override async Task ReceiveMessage(CancellationToken cancellationToken = default)
        {
            if (!TryReceive(MessageQueueTransactionType.None, out var message))
            {
                return;
            }

            if (!TryExtractHeaders(message, out var headers))
            {
                MovePoisonMessageToErrorQueue(message, IsQueuesTransactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);
                return;
            }

            var transportTransaction = new TransportTransaction();
            var context = new ContextBag();

            context.Set(message);

            var length = (int)message.BodyStream.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                _ = await message.BodyStream.ReadAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
                var body = buffer.AsMemory(0, length);

                try
                {
                    await TryProcessMessage(message.Id, headers, body, transportTransaction, context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    await HandleError(message, body, ex, transportTransaction, 1, context, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
