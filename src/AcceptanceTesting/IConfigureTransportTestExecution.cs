public interface IConfigureTransportTestExecution : IConfigureEndpointTestExecution
{
    BridgeTransportDefinition GetBridgeTransport();
}
