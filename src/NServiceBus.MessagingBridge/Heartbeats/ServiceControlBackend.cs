namespace NServiceBus.MessagingBridge.Heartbeats;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Performance.TimeToBeReceived;
using Routing;
using Transport;

class ServiceControlBackend(string destinationQueue, ReceiveAddresses receiveAddresses)
{
    public Task Send(
        object messageToSend,
        TimeSpan timeToBeReceived,
        IMessageDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        var type = messageToSend.GetType();
        var body = Serialize(messageToSend, type);
        return Send(body, type.FullName, timeToBeReceived, dispatcher, cancellationToken);
    }

    internal static byte[] Serialize(object messageToSend, Type type) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageToSend, type));

    Task Send(byte[] body,
        string messageType,
        TimeSpan timeToBeReceived,
        IMessageDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>
        {
            [Headers.EnclosedMessageTypes] = messageType,
            [Headers.ContentType] = ContentTypes.Json,
            [Headers.MessageIntent] = sendIntent
        };

        if (receiveAddresses != null)
        {
            headers[Headers.ReplyToAddress] = receiveAddresses.MainReceiveAddress;
        }

        var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);

        var properties = new DispatchProperties
        {
            DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived)
        };

        var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), properties);

        return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
    }

    readonly string sendIntent = MessageIntent.Send.ToString();
}