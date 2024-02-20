using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;

var app = Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss ";
        });
    })
    .UseNServiceBusBridge((ctx, bridgeConfiguration) =>
    {
        var n1 = new BridgeTransport(
            new SqlServerTransport(@"Server=.\SqlExpress;Database=N1;Integrated Security=true;")
            {
                TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
            })
        {
            Name = $"SQL-N1",
            AutoCreateQueues = true
        };

        n1.SendHeartbeatTo("error");

        n1.ReportCustomChecksTo("error");

        n1.HasEndpoint("N1");

        var n2 = new BridgeTransport(
            new SqlServerTransport(@"Server=.\SqlExpress;Database=N2;Integrated Security=true;")
            {
                TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
            })
        {
            Name = $"SQL-N2",
            AutoCreateQueues = true
        };

        n2.SendHeartbeatTo("error");

        n2.ReportCustomChecksTo("error");

        n2.HasEndpoint("N2");

        bridgeConfiguration.AddTransport(n1);
        bridgeConfiguration.AddTransport(n2);
    })
    .Build();

await app.RunAsync()
    .ConfigureAwait(false);
