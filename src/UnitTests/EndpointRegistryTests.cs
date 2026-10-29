using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;
using UnitTests;

public class EndpointRegistryTests
{
    [Test]
    public async Task Should_dispatch_to_custom_queue_address()
    {
        var destination = await DispatchToEndpoint(new BridgeEndpoint("Receiver", "Receiver@RemoteMachine"));

        Assert.That(destination, Is.EqualTo("Receiver@RemoteMachine"));
    }

    [Test]
    public async Task Should_dispatch_to_transport_address_when_no_custom_queue_address()
    {
        var destination = await DispatchToEndpoint(new BridgeEndpoint("Receiver"));

        Assert.That(destination, Is.EqualTo("Receiver@BridgeMachine"));
    }

    static async Task<string> DispatchToEndpoint(BridgeEndpoint endpoint)
    {
        var targetTransport = new DispatchCapturingTransport();
        var target = new BridgeTransport(targetTransport) { Name = "Target" };
        target.HasEndpoint(endpoint);
        var source = new BridgeTransport(new DispatchCapturingTransport()) { Name = "Source" };

        var serviceProvider = new ServiceCollection()
            .AddSingleton<ILogger<MessageShovelErrorHandlingPolicy>>(new FakeLogger<MessageShovelErrorHandlingPolicy>())
            .BuildServiceProvider();
        var endpointProxyFactory = new EndpointProxyFactory(serviceProvider, new FinalizedBridgeConfiguration([], false));
        var registry = new EndpointRegistry(endpointProxyFactory, new FakeLogger<StartableBridge>());

        await registry.Initialize([source, target], CancellationToken.None);

        var message = new OutgoingMessage("some-id", [], ReadOnlyMemory<byte>.Empty);
        await registry.GetTargetEndpointDispatcher(endpoint.Name).Dispatch(message, new TransportTransaction());

        return targetTransport.DispatchedOperations.UnicastTransportOperations.Single().Destination;
    }

    // Mimics MSMQ: logical addresses resolve to a queue on the bridge's own machine.
    class DispatchCapturingTransport() : TransportDefinition(TransportTransactionMode.ReceiveOnly, true, true, true)
    {
        public TransportOperations DispatchedOperations { get; set; }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
        [
            TransportTransactionMode.ReceiveOnly
        ];

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransportInfrastructure>(new DispatchCapturingTransportInfrastructure(this, receivers));
    }

    class DispatchCapturingTransportInfrastructure : TransportInfrastructure
    {
        public DispatchCapturingTransportInfrastructure(DispatchCapturingTransport transport, ReceiveSettings[] receiverSettings)
        {
            Dispatcher = new DispatchCapturingDispatcher(transport);
            Receivers = receiverSettings.ToDictionary(r => r.Id, r => (IMessageReceiver)new NoopMessageReceiver(r.Id));
        }

        public override Task Shutdown(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override string ToTransportAddress(QueueAddress address) => $"{address.BaseAddress}@BridgeMachine";
    }

    class DispatchCapturingDispatcher(DispatchCapturingTransport transport) : IMessageDispatcher
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            transport.DispatchedOperations = outgoingMessages;
            return Task.CompletedTask;
        }
    }

    class NoopMessageReceiver(string id) : IMessageReceiver
    {
        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => null;

        public string Id { get; } = id;

        public string ReceiveAddress { get; } = id;
    }
}