using System;
using System.Collections.Generic;
using System.Linq;
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

    internal IReadOnlyCollection<TransportConfiguration> TransportConfigurations => transportConfigurations;

    readonly List<TransportConfiguration> transportConfigurations = new List<TransportConfiguration>();
}
