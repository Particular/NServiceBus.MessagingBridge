using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var rc = new MessageRouterConfiguration();

        rc.AddTransport(new MsmqTransport())
            .HasEndpoint("Sales") //.AtMachine("ServerA")
            .HasEndpoint("Finance") //.AtMachine("ServerB");
            .RegisterPublisher("MyNamespace.MyEvent", "Shipping");

        rc.AddTransport(new LearningTransport())
            .HasEndpoint("Shipping")
            .HasEndpoint("Marketing");

        await Host.CreateDefaultBuilder()
             .ConfigureLogging(logging =>
             {
                 logging.ClearProviders();
                 logging.AddConsole();
                 logging.AddEventLog();
             })
            .UseRouter(_ => rc)
            .Build()
            .RunAsync().ConfigureAwait(false);
    }
}