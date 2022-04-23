using NServiceBus;
using NServiceBus.Transport;

public class TestableBridgeTransport : BridgeTransport
{
    public TestableBridgeTransport(TransportDefinition transportDefinition) : base(transportDefinition)
    {
        AutoCreateQueues = true;
    }
}
