using NServiceBus.Transport;

class TransferContext
{
    public TransferContext(string proxyEndpointName, QueueAddress proxyQueueAddress, MessageContext messageToTransfer)
    {
        ProxyEndpointName = proxyEndpointName;
        ProxyQueueAddress = proxyQueueAddress;
        MessageToTransfer = messageToTransfer;
    }

    public string ProxyEndpointName { get; }
    public QueueAddress ProxyQueueAddress { get; }
    public MessageContext MessageToTransfer { get; }
}