using NServiceBus.Transport;

class TransferContext
{
    public TransferContext(
        string proxyEndpointName,
        QueueAddress proxyQueueAddress,
        MessageContext messageToTransfer,
        bool passTransportTransaction)
    {
        ProxyEndpointName = proxyEndpointName;
        ProxyQueueAddress = proxyQueueAddress;
        MessageToTransfer = messageToTransfer;
        PassTransportTransaction = passTransportTransaction;
    }

    public string ProxyEndpointName { get; }
    public QueueAddress ProxyQueueAddress { get; }
    public MessageContext MessageToTransfer { get; }
    public bool PassTransportTransaction { get; }
}