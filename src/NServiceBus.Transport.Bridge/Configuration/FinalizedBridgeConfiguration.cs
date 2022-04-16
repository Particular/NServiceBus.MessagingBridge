using System.Collections.Generic;
using NServiceBus;

class FinalizedBridgeConfiguration
{
    public FinalizedBridgeConfiguration(IReadOnlyCollection<BridgeTransport> transportConfigurations)
        => TransportConfigurations = transportConfigurations;

    public IReadOnlyCollection<BridgeTransport> TransportConfigurations { get; }
}