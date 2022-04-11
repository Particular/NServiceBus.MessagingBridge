using System.Collections.Generic;
using NServiceBus;

class FinalizedBridgeConfiguration
{
    public FinalizedBridgeConfiguration(IReadOnlyCollection<BridgeTransportConfiguration> transportConfigurations)
        => TransportConfigurations = transportConfigurations;

    public IReadOnlyCollection<BridgeTransportConfiguration> TransportConfigurations { get; }
}