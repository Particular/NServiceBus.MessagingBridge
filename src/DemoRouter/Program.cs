using System.Threading.Tasks;
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
            .UseRouter(_ =>
            {
                var rc = new RouterConfiguration();

                rc.AddTransport(new MsmqTransport())
                    .HasEndpoint("Sales") //.AtMachine("ServerA")
                    .HasEndpoint("Finance") //.AtMachine("ServerB");
                    .RegisterPublisher("MyNamespace.MyEvent", "Shipping");

                rc.AddTransport(new LearningTransport())
                    .HasEndpoint("Shipping")
                    .HasEndpoint("Marketing");

                return rc;
            })
            .Build()
            .RunAsync().ConfigureAwait(false);
    }
}