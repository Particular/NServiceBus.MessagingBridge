using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        await Host.CreateDefaultBuilder()
             .ConfigureLogging(logging =>
             {
                 logging.ClearProviders();
                 logging.AddConsole();
                 logging.AddEventLog();
             })
            .UseNServiceBusBridge((ctx, rc) =>
            {
                // demo use of IConfiguration
                var settings = ctx.Configuration.GetSection("Bridge").Get<BridgeSettings>();

                rc.AddTransport(new MsmqTransport(), concurrency: settings.Concurrency, errorQueue: settings.ErrorQueue)
                    .HasEndpoint("Sales") //.AtMachine("ServerA")
                    .HasEndpoint("Finance") //.AtMachine("ServerB");
                    .RegisterPublisher("MyNamespace.MyEvent", "Shipping");

                rc.AddTransport(new LearningTransport(), concurrency: settings.Concurrency, errorQueue: settings.ErrorQueue)
                    .HasEndpoint("Shipping")
                    .HasEndpoint("Marketing");
            })
            .Build()
            .RunAsync().ConfigureAwait(false);
    }
}