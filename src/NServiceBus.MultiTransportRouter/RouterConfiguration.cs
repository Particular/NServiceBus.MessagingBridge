using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using NServiceBus.Transport;

public class RouterConfiguration
{
    public TransportConfiguration AddTransport(TransportDefinition transportDefinition)
    {
        var transportConfiguration = new TransportConfiguration(transportDefinition);
        transports.Add(transportConfiguration);
        return transportConfiguration;
    }

    public FinalizedRouterConfiguration Finalize(IConfiguration configuration)
    {
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        return new FinalizedRouterConfiguration(transports);
    }

    void ApplyConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetSection("Router").Get<RouterSettings>();

        if (settings == null)
        {
            Console.WriteLine("No router settings found");
            return;
        }

        Console.WriteLine(settings.Transports.Count);
    }

    readonly List<TransportConfiguration> transports = new List<TransportConfiguration>();
}
