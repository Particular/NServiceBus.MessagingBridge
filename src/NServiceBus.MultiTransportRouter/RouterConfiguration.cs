using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NServiceBus.Transport;

public class RouterConfiguration
{
    public TransportConfiguration AddTransport(TransportDefinition transportDefinition)
    {
        var transportConfiguration = new TransportConfiguration(transportDefinition);
        transports.Add(transportConfiguration);

        return transportConfiguration;
    }

    public Task<RunningRouter> Start(
         ILoggerFactory loggerFactory = null,
         IConfiguration configuration = null,
         CancellationToken cancellationToken = default)
    {
        var lf = loggerFactory ?? new Microsoft.Extensions.Logging.LoggerFactory();

        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        var startableRouter = new StartableRouter(transports);

        return startableRouter.Start(lf, cancellationToken);
    }

    void ApplyConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetRequiredSection("Router").Get<RouterSettings>();
        Console.WriteLine(settings.Transports.Count);
    }

    List<TransportConfiguration> transports = new List<TransportConfiguration>();
}
