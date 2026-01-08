using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;
using NServiceBus.Transport.Msmq;
using NServiceBus.Unicast.Queuing;
using NUnit.Framework;

class Remote_msmq_address_usage
{
    [Test]
    public async Task Should_use_remote_address_when_dispatching()
    {
        var endpointName = $"RemoteEndpoint-{Guid.NewGuid():N}";
        var remoteMachine = $"remote-{Guid.NewGuid():N}";
        var queueName = $"queue-{Guid.NewGuid():N}";
        var explicitRemoteAddress = $"{queueName}@{remoteMachine}";

        var endpointRegistry = BuildRegistry();
        string destinationCaptured = null;
        string errorMessageCaptured = null;
        var transportDefinition = new MsmqTransport
        {
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly,
            OnSendCallbackForTesting = (_, operation) =>
            {
                destinationCaptured = operation.Destination;
                var message = $"Queue '{operation.Destination}' not found.";
                errorMessageCaptured = message;
                throw new QueueNotFoundException(operation.Destination, message, null);
            }
        };
        var bridgeTransport = new BridgeTransport(transportDefinition);
        bridgeTransport.HasEndpoint(endpointName, explicitRemoteAddress);

        await endpointRegistry.Initialize(new[] { bridgeTransport });

        var dispatcher = endpointRegistry.GetTargetEndpointDispatcher(endpointName);
        var headers = new Dictionary<string, string>
        {
            [Headers.MessageIntent] = nameof(MessageIntent.Send)
        };

        var exception = Assert.ThrowsAsync<QueueNotFoundException>(async () =>
        {
            await dispatcher.Dispatch(new OutgoingMessage(Guid.NewGuid().ToString(), headers, Array.Empty<byte>()), new TransportTransaction(), CancellationToken.None);
        });

        Assert.Multiple(() =>
        {
            Assert.That(errorMessageCaptured, Does.Contain(remoteMachine));
            Assert.That(destinationCaptured, Is.EqualTo(explicitRemoteAddress));
        });
        Assert.That(destinationCaptured, Is.EqualTo(explicitRemoteAddress));
    }

    static EndpointRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageShovel, NoopMessageShovel>();
        services.AddSingleton<ILogger<MessageShovelErrorHandlingPolicy>>(NullLogger<MessageShovelErrorHandlingPolicy>.Instance);

        var serviceProvider = services.BuildServiceProvider();

        return new EndpointRegistry(new EndpointProxyFactory(serviceProvider), NullLogger<StartableBridge>.Instance);
    }

    class NoopMessageShovel : IMessageShovel
    {
        public Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

