﻿namespace NServiceBus.MessagingBridge.Heartbeats;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Raw;
using ServiceControl.Plugin.Heartbeat.Messages;
using Transport;

/// <summary>
/// Used to send Heartbeats to ServiceControl
/// </summary>
public class ServiceControlHeartbeatSender
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="serviceControlQueue"></param>
    /// <param name="bridgeTransportName"></param>
    /// <param name="transportDefinition"></param>
    /// <param name="hostId"></param>
    public ServiceControlHeartbeatSender(string serviceControlQueue, string bridgeTransportName, TransportDefinition transportDefinition, Guid hostId)
    {
        this.serviceControlQueue = serviceControlQueue;
        this.transportDefinition = transportDefinition;
        this.bridgeTransportName = bridgeTransportName;
        this.hostId = hostId;
    }
    /// <summary>
    /// SendHeartbeat to ServiceControl
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task SendHeartbeat(CancellationToken cancellationToken = default)
    {
        var rawEndpointConfiguration =
            RawEndpointConfiguration.CreateSendOnly("Bridge", transportDefinition);

        var rawEndpoint = await RawEndpoint.Create(rawEndpointConfiguration, cancellationToken).ConfigureAwait(false);

        var targetEndpointDispatcher = new TargetEndpointDispatcher(bridgeTransportName, rawEndpoint, serviceControlQueue);

        var endpointHeartbeat = new EndpointHeartbeat()
        {
            EndpointName = "MessagingBridge",
            ExecutedAt = DateTime.UtcNow,
            Host = "Laptop2",
            HostId = hostId
        };

        var headers = new Dictionary<string, string>()
        {
            [Headers.EnclosedMessageTypes] = endpointHeartbeat.GetType().FullName,
            [Headers.ContentType] = ContentTypes.Json,
            [Headers.MessageIntent] = "Send"
        };

        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(endpointHeartbeat));

        var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, messageBody);

        await targetEndpointDispatcher.Dispatch(outgoingMessage, new TransportTransaction(), cancellationToken)
            .ConfigureAwait(false);
    }

    readonly string serviceControlQueue;
    readonly TransportDefinition transportDefinition;
    readonly string bridgeTransportName;
    readonly Guid hostId;
}