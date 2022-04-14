using NServiceBus.Transport;

class TransferContext
{
    public TransferContext(
        string sourceTransport,
        string proxyEndpointName,
        MessageContext messageToTransfer,
        bool passTransportTransaction)
    {
        SourceTransport = sourceTransport;
        SourceEndpointName = proxyEndpointName;
        MessageToTransfer = messageToTransfer;
        PassTransportTransaction = passTransportTransaction;
    }

    public string SourceTransport { get; }
    public string SourceEndpointName { get; }
    public MessageContext MessageToTransfer { get; }
    public bool PassTransportTransaction { get; }
}