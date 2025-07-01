using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class BridgeComponent<TContext>(
    Action<BridgeConfiguration, BridgeTransport> bridgeConfigurationAction,
    Action<PublisherMetadata> publisherMetadataAction,
    IConfigureBridgeTestExecution configureBridge) : IComponentBehavior
    where TContext : BridgeScenarioContext
{
#pragma warning disable PS0018
    public Task<ComponentRunner> CreateRunner(RunDescriptor run) => Task.FromResult<ComponentRunner>(new Runner(bridgeConfigurationAction, publisherMetadataAction, (TContext)run.ScenarioContext, configureBridge, new AcceptanceTestLoggerFactory(run.ScenarioContext)));
#pragma warning restore PS0018

    class Runner(
        Action<BridgeConfiguration, BridgeTransport> bridgeConfigurationAction,
        Action<PublisherMetadata> publisherMetadataAction,
        TContext scenarioContext,
        IConfigureBridgeTestExecution configureBridge,
        ILoggerFactory loggerFactory)
        : ComponentRunner
    {
        public override string Name => "Bridge";

        public override async Task Start(CancellationToken cancellationToken = default)
        {
            var builder = Host.CreateApplicationBuilder();

            var publisherMetadata = new PublisherMetadata();
            publisherMetadataAction?.Invoke(publisherMetadata);

            bridgeTransport = configureBridge.Configure(publisherMetadata);
            scenarioContext.Initialize(bridgeTransport);

            var bridgeConfiguration = new BridgeConfiguration();
            bridgeConfigurationAction(bridgeConfiguration, bridgeTransport);

            builder.UseNServiceBusBridge(bridgeConfiguration);

            builder.Services.AddSingleton(loggerFactory);
            builder.Services.RemoveAll(typeof(IMessageShovel));
            builder.Services.AddTransient<MessageShovel>();
            builder.Services.AddTransient<FakeShovel>();
            builder.Services.AddTransient<IMessageShovel, FakeShovel>();

            host = builder.Build();

            await host.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task Stop(CancellationToken cancellationToken = default)
        {
            if (host is null)
            {
                return;
            }

            try
            {
                await host.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                host.Dispose();
                await configureBridge.Cleanup(bridgeTransport).ConfigureAwait(false);
            }
        }

        IHost host;
        BridgeTransport bridgeTransport;
    }
}