// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;
using NServiceBus;

//const string HostId = "{4701FAF9-39A7-4033-9AA9-D95C0DFC0480}";

//var sender = new NServiceBus.MessagingBridge.Heartbeats.ServiceControlHeartbeatSender
//    ("Particular.n3-bridge-test",
//    "bridge",
//    new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "amqp://guest:guest@localhost:5672/n3-bridge-test"),
//    new Guid(HostId));

//var tokenSource = new CancellationTokenSource(20000);

//while (!tokenSource.Token.IsCancellationRequested)
//{
//    await sender.SendHeartbeat(new CancellationToken()).ConfigureAwait(false);

//    await Task.Delay(100).ConfigureAwait(true);
//}

await Host.CreateDefaultBuilder()
   .UseNServiceBusBridge((ctx, bridgeConfiguration) =>
   {
       var n2 = new BridgeTransport(
               new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "amqp://guest:guest@localhost:5672/n2-bridge-test"))
       {
           Name = $"SQL-N2",
           AutoCreateQueues = true
       };

       n2.HasEndpoint("N2");

       var n3 = new BridgeTransport(
                         new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "amqp://guest:guest@localhost:5672/n3-bridge-test"))
       {
           Name = $"SQL-N3",
           AutoCreateQueues = true
       };

       n3.HasEndpoint("N3");

       bridgeConfiguration.AddTransport(n2);
       bridgeConfiguration.AddTransport(n3);
   }
).Build()
.RunAsync().ConfigureAwait(false);
