// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;
using NServiceBus;

await Host.CreateDefaultBuilder()
   .UseNServiceBusBridge((ctx, bridgeConfiguration) =>
   {
       var n1 = new BridgeTransport(
        new SqlServerTransport(@"Data Source=localhost;Initial Catalog=N1;User ID=sa;Password=yourStrong(!)Password;Max Pool Size=100")
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        })
       {
           Name = $"SQL-N1",
       };

       n1.SendHeartbeatTo("Particular.n1-bridge-test@[dbo]@[N1]", TimeSpan.FromSeconds(1));
       n1.HasEndpoint("N1");
       n1.AutoCreateQueues = true;

       var n2 = new BridgeTransport(
               new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "amqp://guest:guest@localhost:5672/n2-bridge-test"))
       {
           Name = $"RabbitMQ-N2",
       };

       n2.SendHeartbeatTo("Particular.n2-bridge-test", TimeSpan.FromSeconds(10));
       n2.HasEndpoint("N2");
       n2.AutoCreateQueues = true;

       var n3 = new BridgeTransport(
                         new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "amqp://guest:guest@localhost:5672/n3-bridge-test"))
       {
           Name = $"RabbitMQ-N3",
       };

       n3.SendHeartbeatTo("Particular.n3-bridge-test", TimeSpan.FromSeconds(10));
       n3.HasEndpoint("N3");
       n3.AutoCreateQueues = true;

       bridgeConfiguration.AddTransport(n1);
       bridgeConfiguration.AddTransport(n2);
       bridgeConfiguration.AddTransport(n3);
   }
)
.UseConsoleLifetime()
.Build()
.RunAsync()
.ConfigureAwait(false);
