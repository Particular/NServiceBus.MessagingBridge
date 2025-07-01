using NServiceBus;
using NServiceBus.Transport;

public sealed class TestableBridgeTransport<TTransport> : BridgeTransport
    where TTransport : TransportDefinition
{
    public TestableBridgeTransport(TTransport transportDefinition) : base(transportDefinition)
    {
        AutoCreateQueues = true;
        TransportDefinition = transportDefinition;
    }

    public TTransport TransportDefinition { get; }
}