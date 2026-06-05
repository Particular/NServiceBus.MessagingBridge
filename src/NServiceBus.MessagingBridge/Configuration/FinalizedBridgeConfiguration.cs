namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class FinalizedBridgeConfiguration
{
    public FinalizedBridgeConfiguration(
        IReadOnlyCollection<BridgeTransport> transportConfigurations,
        bool translateReplyToAddressForFailedMessages,
        Func<ICriticalErrorContext, CancellationToken, Task> criticalErrorAction = null)
    {
        TransportConfigurations = transportConfigurations;
        TranslateReplyToAddressForFailedMessages = translateReplyToAddressForFailedMessages;
        CriticalErrorAction = criticalErrorAction;
    }

    public IReadOnlyCollection<BridgeTransport> TransportConfigurations { get; }
    public bool TranslateReplyToAddressForFailedMessages { get; }
    public Func<ICriticalErrorContext, CancellationToken, Task> CriticalErrorAction { get; }
}
