using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NServiceBus.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

class RouterHostedService : IHostedService
{
    public RouterHostedService(
        MessageRouterConfiguration routerConfiguration,
        ILoggerFactory loggerFactory,
        DeferredLoggerFactory deferredLoggerFactory)
    {
        this.loggerFactory = loggerFactory;
        this.deferredLoggerFactory = deferredLoggerFactory;
        this.routerConfiguration = routerConfiguration;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        LogManager.UseFactory(new LoggerFactory(loggerFactory));
        deferredLoggerFactory.FlushAll(loggerFactory);

        router = await routerConfiguration.Start(loggerFactory, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return router.Stop(cancellationToken);
    }

    readonly MessageRouterConfiguration routerConfiguration;
    readonly DeferredLoggerFactory deferredLoggerFactory;
    readonly ILoggerFactory loggerFactory;

    RunningRouter router;
}