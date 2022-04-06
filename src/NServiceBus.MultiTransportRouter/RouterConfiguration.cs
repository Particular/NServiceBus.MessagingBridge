using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NServiceBus.Transport;

public class RouterConfiguration
{
    //TODO: discuss defaults and validation
    public TransportConfiguration AddTransport(
        TransportDefinition transportDefinition,
        string name = null,
        int concurrency = 1,
        string errorQueue = "error",
        bool autoCreateQueues = true)
    {
        var transportConfiguration = new TransportConfiguration(transportDefinition);

        if (!string.IsNullOrEmpty(name))
        {
            transportConfiguration.Name = name;
        }
        else
        {
            transportConfiguration.Name = transportDefinition.GetType().Name.ToLower().Replace("transport", "");
        }

        if (!string.IsNullOrEmpty(errorQueue))
        {
            transportConfiguration.ErrorQueue = errorQueue;
        }

        transportConfiguration.Concurrency = concurrency;

        transportConfiguration.AutoCreateQueues = autoCreateQueues;


        if (transports.Any(t => t.Name == transportConfiguration.Name))
        {
            throw new InvalidOperationException($"A transport with the name {transportConfiguration.Name} has already been configured. Use a different transport type or specify a custom name");
        }

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
