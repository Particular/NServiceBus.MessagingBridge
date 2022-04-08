namespace NServiceBus
{

    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BridgeConfiguration
    {
        public void AddTransport(BridgeTransportConfiguration transportConfiguration)
        {
            if (transportConfigurations.Any(t => t.Name == transportConfiguration.Name))
            {
                throw new InvalidOperationException($"A transport with the name {transportConfiguration.Name} has already been configured. Use a different transport type or specify a custom name");
            }

            transportConfigurations.Add(transportConfiguration);
        }

        internal void Validate()
        {
            if (transportConfigurations.Count < 2)
            {
                throw new InvalidOperationException("At least two transports needs to be configured");
            }

        }

        internal IReadOnlyCollection<BridgeTransportConfiguration> TransportConfigurations => transportConfigurations;

        readonly List<BridgeTransportConfiguration> transportConfigurations = new List<BridgeTransportConfiguration>();
    }
}