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
class AddressMap : IAddressMap
{
    readonly HashSet<string> endpoints = new HashSet<string>();
    readonly IReadOnlyDictionary<string, IStartableRawEndpoint> dispatchers;
    readonly Dictionary<string, TransportAddressMap> addressMap;

    public AddressMap(IReadOnlyDictionary<string, IStartableRawEndpoint> dispatchers)
    {
        this.dispatchers = dispatchers;
        addressMap = dispatchers.ToDictionary(dispatcher => dispatcher.Key, _ => new TransportAddressMap());
    }

    public void Add(BridgeTransport transport, BridgeEndpoint endpoint)
    {
        if (!endpoints.Add(endpoint.Name))
        {
            throw new InvalidOperationException($"{endpoint.Name} has already been added to the address map.");
        }

        var queueAddress = new QueueAddress(endpoint.Name);

        foreach (var targetTransport in addressMap.Keys)
        {
            var targetAddress = DetermineTransportAddress(targetTransport, transport.Name, endpoint.QueueAddress, queueAddress);

            foreach (var sourceTransport in addressMap.Keys)
            {
                var sourceAddress = DetermineTransportAddress(sourceTransport, transport.Name, endpoint.QueueAddress, queueAddress);

                addressMap[targetTransport][sourceAddress] = targetAddress;
            }
        }
    }

    string DetermineTransportAddress(string transportToMap, string endpointTransport, string overriddenQueueAddress, QueueAddress queueAddress) => transportToMap == endpointTransport
        ? overriddenQueueAddress ?? dispatchers[transportToMap].ToTransportAddress(queueAddress)
        : dispatchers[transportToMap].ToTransportAddress(queueAddress);

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
