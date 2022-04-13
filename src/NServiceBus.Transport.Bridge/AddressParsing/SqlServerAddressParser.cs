using System.Linq;

class SqlServerAddressParser : ITransportAddressParser
{
    public string ParseEndpointName(string address)
    {
        return address.Split('@').First();
    }
}