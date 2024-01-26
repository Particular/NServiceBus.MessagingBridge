// See https://aka.ms/new-console-template for more information

using NServiceBus;


var sender = new NServiceBus.MessagingBridge.Heartbeats.ServiceControlHeartbeatSender
    ("error",
    "bridge",
    new LearningTransport() { StorageDirectory = @"P:\learning-transport" });

await sender.SendHeartbeat(new CancellationToken()).ConfigureAwait(false);
