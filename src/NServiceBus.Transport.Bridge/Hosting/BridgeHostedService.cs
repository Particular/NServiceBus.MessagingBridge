using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NServiceBus.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

class BridgeHostedService : IHostedService
{
    public BridgeHostedService(
        StartableRouter startableRouter,
        ILoggerFactory loggerFactory,
        DeferredLoggerFactory deferredLoggerFactory)
    {
        this.loggerFactory = loggerFactory;
        this.deferredLoggerFactory = deferredLoggerFactory;
        this.startableRouter = startableRouter;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        LogManager.UseFactory(new LoggerFactory(loggerFactory));
        deferredLoggerFactory.FlushAll(loggerFactory);

        router = await startableRouter.Start(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return router.Stop(cancellationToken);
    }

    readonly StartableRouter startableRouter;
    readonly DeferredLoggerFactory deferredLoggerFactory;
    readonly ILoggerFactory loggerFactory;

    RunningRouter router;
}