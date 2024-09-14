using System.Collections.Generic;

/// <summary>
/// How to map an address depends upon which transport the message is being sent to, 
/// so it is necessary to have an address mapping dictionary for each transport
/// </summary>
class AddressMap
{
    readonly Dictionary<string, TransportAddressMap> addressMap = [];

    public void Add(string transport, string address, string translatedAddress)
    {
        if (!addressMap.TryGetValue(transport, out var transportAddressMappings))
        {
            transportAddressMappings = [];
            addressMap.Add(transport, transportAddressMappings);
        }

        transportAddressMappings.Add(address, translatedAddress);
    }

    public bool TryTranslate(string targetTransport, string address, out string bestMatch)
    {
        var transportAddressMappings = addressMap[targetTransport];

        if (transportAddressMappings.TryGetValue(address, out bestMatch))
        {
            return true;
        }

        bestMatch = address.GetClosestMatch(transportAddressMappings.Keys);
        return false;
    }
}

class TransportAddressMap : Dictionary<string, string>
{
}
