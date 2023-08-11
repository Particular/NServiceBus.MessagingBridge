using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NServiceBus.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

class BridgeHostedService : IHostedService
{
    public BridgeHostedService(
        IStartableBridge startableBridge,
        ILoggerFactory loggerFactory,
        DeferredLoggerFactory deferredLoggerFactory)
    {
        this.loggerFactory = loggerFactory;
        this.deferredLoggerFactory = deferredLoggerFactory;
        this.startableBridge = startableBridge;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        LogManager.UseFactory(new LoggerFactory(loggerFactory));
        deferredLoggerFactory.FlushAll(loggerFactory);

        runningBridge = await startableBridge.Start(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => runningBridge.Stop(cancellationToken);

    IStoppableBridge runningBridge;

    readonly IStartableBridge startableBridge;
    readonly DeferredLoggerFactory deferredLoggerFactory;
    readonly ILoggerFactory loggerFactory;
}