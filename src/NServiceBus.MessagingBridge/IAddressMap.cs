interface IAddressMap
{
    void Add(string transport, string address, string translatedAddress);

    bool TryTranslate(string targetTransport, string address, out string bestMatch);
}
