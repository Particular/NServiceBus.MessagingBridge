using NServiceBus;

interface IAddressMap
{
    void Add(BridgeTransport transport, BridgeEndpoint endpoint);

    bool TryTranslate(string targetTransport, string address, out string bestMatch);
}
