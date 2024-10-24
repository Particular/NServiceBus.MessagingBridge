﻿namespace NServiceBus;

using System.Collections.Generic;

class FinalizedBridgeConfiguration
{
    public FinalizedBridgeConfiguration(IReadOnlyCollection<BridgeTransport> transportConfigurations, bool translateReplyToAddressForFailedMessages)
    {
        TransportConfigurations = transportConfigurations;
        TranslateReplyToAddressForFailedMessages = translateReplyToAddressForFailedMessages;
    }

    public IReadOnlyCollection<BridgeTransport> TransportConfigurations { get; }
    public bool TranslateReplyToAddressForFailedMessages { get; }
}