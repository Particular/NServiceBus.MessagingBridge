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
        LogManager.UseFactory(new LoggerFactory(loggerFactory));
        deferredLoggerFactory.FlushAll(loggerFactory);

        runningBridge = await startableBridge.Start(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => runningBridge?.Stop(cancellationToken) ?? Task.CompletedTask;

    IStoppableBridge runningBridge;
}