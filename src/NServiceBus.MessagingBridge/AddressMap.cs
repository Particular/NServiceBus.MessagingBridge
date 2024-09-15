using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;

/// <summary>
/// How to map an address depends upon which transport the message is being sent to, 
/// so it is necessary to have an address mapping dictionary for each transport
/// </summary>
class AddressMap(IReadOnlyDictionary<string, IStartableRawEndpoint> dispatchers) : IAddressMap
{
    readonly Dictionary<string, TransportAddressMap> addressMap =
        dispatchers.ToDictionary(dispatcher => dispatcher.Key, _ => new TransportAddressMap());

    readonly HashSet<string> endpoints = [];

    public void Add(BridgeTransport transport, BridgeEndpoint endpoint)
    {
        if (!endpoints.Add(endpoint.Name))
        {
            throw new InvalidOperationException($"{endpoint.Name} has already been added to the address map.");
        }

        foreach (var targetTransport in addressMap.Keys)
        {
            var queueAddress = new QueueAddress(endpoint.Name);
            var targetAddress = targetTransport == transport.Name
                ? endpoint.QueueAddress ?? dispatchers[targetTransport].ToTransportAddress(queueAddress)
                : dispatchers[targetTransport].ToTransportAddress(queueAddress);

            foreach (var sourceTransport in addressMap.Keys)
            {
                var sourceAddress = sourceTransport == transport.Name
                    ? endpoint.QueueAddress ?? dispatchers[sourceTransport].ToTransportAddress(queueAddress)
                    : dispatchers[sourceTransport].ToTransportAddress(queueAddress);

                addressMap[targetTransport][sourceAddress] = targetAddress;
            }
        }
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
