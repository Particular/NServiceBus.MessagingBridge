class NoOpAddressParser : ITransportAddressParser
{
    public string ParseEndpointName(string address) => address;
}