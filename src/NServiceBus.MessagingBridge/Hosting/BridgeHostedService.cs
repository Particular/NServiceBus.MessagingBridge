using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NServiceBus.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

class BridgeHostedService(
    IStartableBridge startableBridge,
    ILoggerFactory loggerFactory,
    DeferredLoggerFactory deferredLoggerFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        LogManager.UseFactory(new LoggerFactory(loggerFactory));
#pragma warning restore CS0618 // Type or member is obsolete
        deferredLoggerFactory.FlushAll(loggerFactory);

        runningBridge = await startableBridge.Start(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => runningBridge?.Stop(cancellationToken) ?? Task.CompletedTask;

    IStoppableBridge runningBridge;
}