namespace NServiceBus;

using System.Collections.Generic;

interface IFinalizedBridgeConfiguration
{
    public IReadOnlyCollection<BridgeTransport> TransportConfigurations { get; }
    public bool TranslateReplyToAddressForFailedMessages { get; }
}