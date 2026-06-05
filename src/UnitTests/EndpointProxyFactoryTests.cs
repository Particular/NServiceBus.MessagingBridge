using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;
using NUnit.Framework;
using UnitTests;

public class EndpointProxyFactoryTests
{
    [TestCase(EndpointKind.Dispatcher)]
    [TestCase(EndpointKind.Proxy)]
    public async Task Should_invoke_configured_critical_error_action_when_transport_raises_critical_error(EndpointKind endpointKind)
    {
        var criticalErrorContext = new TaskCompletionSource<ICriticalErrorContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exception = new Exception("Something failed");

        Task OnCriticalError(ICriticalErrorContext context, CancellationToken cancellationToken)
        {
            criticalErrorContext.SetResult(context);
            return Task.CompletedTask;
        }

        var bridgeConfiguration = new FinalizedBridgeConfiguration([], false, OnCriticalError);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IMessageShovel>(new TestMessageShovel())
            .AddSingleton<ILogger<MessageShovelErrorHandlingPolicy>>(new FakeLogger<MessageShovelErrorHandlingPolicy>())
            .BuildServiceProvider();
        var endpointProxyFactory = new EndpointProxyFactory(serviceProvider, bridgeConfiguration);
        var transport = new CriticalErrorCapturingTransport();
        var bridgeTransport = new BridgeTransport(transport);
        var startableEndpoint = endpointKind == EndpointKind.Dispatcher
            ? await endpointProxyFactory.CreateDispatcher(bridgeTransport, CancellationToken.None)
            : await endpointProxyFactory.CreateProxy(new BridgeEndpoint("Sales"), bridgeTransport, CancellationToken.None);

        var runningEndpoint = await startableEndpoint.Start(CancellationToken.None);

        transport.RaiseCriticalError("Critical error", exception, CancellationToken.None);

        var context = await criticalErrorContext.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(context.Error, Is.EqualTo("Critical error"));
            Assert.That(context.Exception, Is.SameAs(exception));
        });

        await runningEndpoint.Stop(CancellationToken.None);
    }

    public enum EndpointKind
    {
        Dispatcher,
        Proxy
    }

    class CriticalErrorCapturingTransport : TransportDefinition
    {
        public CriticalErrorCapturingTransport()
            : base(TransportTransactionMode.ReceiveOnly, true, true, true)
        {
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
        [
            TransportTransactionMode.ReceiveOnly
        ];

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            criticalErrorAction = hostSettings.CriticalErrorAction;

            return Task.FromResult<TransportInfrastructure>(new TestTransportInfrastructure(receivers));
        }

        public void RaiseCriticalError(string message, Exception exception, CancellationToken cancellationToken = default) =>
            criticalErrorAction(message, exception, cancellationToken);

        Action<string, Exception, CancellationToken> criticalErrorAction;
    }

    class TestTransportInfrastructure : TransportInfrastructure
    {
        public TestTransportInfrastructure(ReceiveSettings[] receiverSettings)
        {
            Dispatcher = new TestMessageDispatcher();
            var receivers = new Dictionary<string, IMessageReceiver>();
            foreach (var receiverSetting in receiverSettings)
            {
                receivers.Add(receiverSetting.Id, new TestMessageReceiver(receiverSetting.Id));
            }

            Receivers = receivers;
        }

        public override Task Shutdown(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override string ToTransportAddress(QueueAddress address) => address.ToString();
    }

    class TestMessageDispatcher : IMessageDispatcher
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    class TestMessageReceiver(string id) : IMessageReceiver
    {
        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => null;

        public string Id { get; } = id;

        public string ReceiveAddress { get; } = id;
    }

    class TestMessageShovel : IMessageShovel
    {
        public Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
