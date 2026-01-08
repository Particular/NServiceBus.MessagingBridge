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
using NUnit.Framework;

// Guards against regressions where EndpointRegistry ignores BridgeEndpoint.QueueAddress when explicitly provided.
// Remote MSMQ addresses rely on this behavior to dispatch to queue@remoteMachine instead of queue@localMachine.
public class EndpointRegistryTests
{
    [Test]
    public async Task Should_use_explicit_queue_address_when_provided()
    {
        var endpointName = $"endpoint-{Guid.NewGuid():N}";
        var explicitAddress = $"queue-{Guid.NewGuid():N}@remote-machine";

        var endpointRegistry = BuildRegistry();

        var bridgeTransport = new BridgeTransport(new RecordingTransportDefinition("queue@local-machine"));
        bridgeTransport.HasEndpoint(endpointName, explicitAddress);

        await endpointRegistry.Initialize(new[] { bridgeTransport });

        Assert.That(endpointRegistry.GetEndpointAddress(endpointName), Is.EqualTo(explicitAddress));
    }

    [Test]
    public async Task Should_use_dispatcher_derived_address_when_queue_address_not_provided()
    {
        var endpointName = $"endpoint-{Guid.NewGuid():N}";
        var dispatcherDerivedAddress = $"queue-{Guid.NewGuid():N}@local-machine";

        var endpointRegistry = BuildRegistry();

        var bridgeTransport = new BridgeTransport(new RecordingTransportDefinition(dispatcherDerivedAddress));
        bridgeTransport.HasEndpoint(endpointName); // No QueueAddress specified

        await endpointRegistry.Initialize(new[] { bridgeTransport });

        Assert.That(endpointRegistry.GetEndpointAddress(endpointName), Is.EqualTo(dispatcherDerivedAddress));
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

    class RecordingTransportDefinition : TransportDefinition
    {
        readonly string transportAddress;

        public RecordingTransportDefinition(string transportAddress)
            : base(TransportTransactionMode.ReceiveOnly, supportsDelayedDelivery: true, supportsPublishSubscribe: true, supportsTTBR: true)
        {
            this.transportAddress = transportAddress;
        }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            var infrastructure = new RecordingTransportInfrastructure(transportAddress);
            return Task.FromResult<TransportInfrastructure>(infrastructure);
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => new[] { TransportTransactionMode.ReceiveOnly };
    }

    class RecordingTransportInfrastructure : TransportInfrastructure
    {
        readonly string transportAddress;

        public RecordingTransportInfrastructure(string transportAddress)
        {
            this.transportAddress = transportAddress;
            Dispatcher = new NoopDispatcher();
            Receivers = new Dictionary<string, IMessageReceiver>();
        }

        public override Task Shutdown(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override string ToTransportAddress(QueueAddress address) => transportAddress;

        class NoopDispatcher : IMessageDispatcher
        {
            public Task Dispatch(TransportOperations transportOperations, TransportTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}

