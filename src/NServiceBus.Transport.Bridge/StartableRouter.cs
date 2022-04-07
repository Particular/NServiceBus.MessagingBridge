using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class StartableRouter
{
    public StartableRouter(
        RouterConfiguration configuration,
        ILogger<StartableRouter> logger,
        IServiceProvider serviceProvider)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    public async Task<RunningBridge> Start(CancellationToken cancellationToken = default)
    {
        var transports = configuration.TransportConfigurations;
        var proxies = new List<EndpointProxy>();

        // Loop through all configured transports
        foreach (var transportConfiguration in transports)
        {
            logger.LogInformation("Starting proxies for transport {name}", transportConfiguration.Name);

            // Get all endpoint-names that I need to fake (host)
            // That is all endpoint-names that I don't have on this transport.
            var endpoints = transports.Where(s => s != transportConfiguration).SelectMany(s => s.Endpoints);

            // Go through all endpoints that we need to fake on our transport
            foreach (var endpointToSimulate in endpoints)
            {
                var endpointProxy = serviceProvider.GetRequiredService<EndpointProxy>();

                await endpointProxy.Start(endpointToSimulate, transportConfiguration, cancellationToken)
                    .ConfigureAwait(false);

                // Find the transport that has my TransportDefinition and attach it
                transports.Single(s => s.TransportDefinition == transportConfiguration.TransportDefinition)
                    .Proxy = endpointProxy;

                proxies.Add(endpointProxy);

                logger.LogInformation("Proxy for endpoint {endpoint} started on {transport}", endpointToSimulate.Name, transportConfiguration.Name);
            }
        }

        logger.LogInformation("Router startup complete");

        return new RunningBridge(proxies);
    }

    readonly RouterConfiguration configuration;
    readonly ILogger<StartableRouter> logger;
    readonly IServiceProvider serviceProvider;
}