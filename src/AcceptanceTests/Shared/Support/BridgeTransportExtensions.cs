using System;
using NServiceBus;
using NServiceBus.Transport;

public static class BridgeTransportExtensions
{
    public static BridgeTransport ToTestableBridge<TTransport>(this TTransport transportDefinition,
        string name = "TestableBridgeTransport")
        where TTransport : TransportDefinition =>
        new TestableBridgeTransport<TTransport>(transportDefinition) { Name = name };

    public static TTransport FromTestableBridge<TTransport>(this BridgeTransport bridgeTransport) where TTransport : TransportDefinition
    {
        var testableBridgeTransport = bridgeTransport as TestableBridgeTransport<TTransport> ?? throw new InvalidOperationException($"The bridge transport is not of type {typeof(TTransport).Name}. It is of type {bridgeTransport.GetType().Name}.");
        return testableBridgeTransport.TransportDefinition;
    }
}