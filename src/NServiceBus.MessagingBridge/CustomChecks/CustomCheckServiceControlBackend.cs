namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Routing;
    using Transport;

    class CustomCheckServiceControlBackend
    {
        public Task Send(object messageToSend, TimeSpan? timeToLive = null,
            CancellationToken cancellationToken = default)
        {
            var body = Serialize(messageToSend);

            return Send(body, messageToSend.GetType().FullName, timeToLive, cancellationToken);
        }

        static byte[] Serialize(object messageToSend) => JsonSerializer.SerializeToUtf8Bytes(messageToSend);

        Task Send(byte[] body, string messageType, TimeSpan? timeToBeReceived, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.EnclosedMessageTypes] = messageType,
                [Headers.ContentType] = ContentTypes.Json,
                [Headers.MessageIntent] = "Send"
            };

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);

            var dispatchProperties = new DispatchProperties();

            if (timeToBeReceived != null)
            {
                dispatchProperties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived.Value);
            }

            var operation =
                new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), dispatchProperties);

            return messageSender?.Dispatch(new TransportOperations(operation), new TransportTransaction(),
                cancellationToken);
        }

        readonly IMessageDispatcher messageSender;

        readonly string destinationQueue;

        public CustomCheckServiceControlBackend(string destinationQueue, IMessageDispatcher messageDispatcher)
        {
            messageSender = messageDispatcher;

            this.destinationQueue = destinationQueue;
        }
    }
}