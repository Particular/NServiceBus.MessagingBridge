using System.Linq;

class MsmqAddressParser : ITransportAddressParser
{
    public string ParseEndpointName(string address)
    {
        return address.Split('@').First();
    }
}