using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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


        if (transportConfigurations.Any(t => t.Name == transportConfiguration.Name))
        {
            throw new InvalidOperationException($"A transport with the name {transportConfiguration.Name} has already been configured. Use a different transport type or specify a custom name");
        }

        transportConfigurations.Add(transportConfiguration);
        return transportConfiguration;
    }

    public FinalizedRouterConfiguration Finalize(IConfiguration configuration, ILogger<RouterConfiguration> logger)
    {
        var settings = configuration.GetSection("Router").Get<RouterSettings>();

        if (settings == null)
        {
            logger.LogInformation("No router settings to apply found in configuration");
            return new FinalizedRouterConfiguration(transportConfigurations);
        }

        foreach (var transportSetting in settings.Transports)
        {
            var transportConfiguration = transportConfigurations.SingleOrDefault(t => t.Name == transportSetting.Name);

            if (transportConfiguration == null)
            {
                throw new InvalidOperationException($"No transport with name {transportSetting.Name} could be found.");
            }

            if (transportSetting.Concurrency > 0)
            {
                transportConfiguration.Concurrency = transportSetting.Concurrency;
            }

        }

        return new FinalizedRouterConfiguration(transportConfigurations);
    }

    readonly List<TransportConfiguration> transportConfigurations = new List<TransportConfiguration>();
}
