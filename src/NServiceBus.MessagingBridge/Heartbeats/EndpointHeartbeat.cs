namespace NServiceBus.MessagingBridge.Heartbeats;

using System;

class EndpointHeartbeat
{
    public DateTime ExecutedAt { get; set; }
    public string EndpointName { get; set; }
    public Guid HostId { get; set; }
    public string Host { get; set; }
}