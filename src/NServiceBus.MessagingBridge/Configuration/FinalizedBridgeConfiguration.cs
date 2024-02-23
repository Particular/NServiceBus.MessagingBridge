namespace NServiceBus
{
    using System.Collections.Generic;

    class FinalizedBridgeConfiguration
    {
        public FinalizedBridgeConfiguration(IReadOnlyCollection<BridgeTransport> transportConfigurations)
            => TransportConfigurations = transportConfigurations;

        public IReadOnlyCollection<BridgeTransport> TransportConfigurations { get; }
    }
}