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
                var settings = ctx.Configuration.GetSection("Bridge").Get<MyBridgeSettings>();

                var msmqTransport = new BridgeTransportConfiguration(new MsmqTransport())
                {
                    Concurrency = settings.Concurrency,
                    ErrorQueue = settings.ErrorQueue
                };

                var financeEndpoint = new BridgeEndpoint("Finance");

                financeEndpoint.RegisterPublisher("MyNamespace.MyEvent", "Shipping");

                msmqTransport.HasEndpoint(financeEndpoint);
                msmqTransport.HasEndpoint("Sales");
                msmqTransport.HasEndpoint("Error");

                rc.AddTransport(msmqTransport);

                var learningTransport = new BridgeTransportConfiguration(new LearningTransport())
                {
                    Concurrency = settings.Concurrency,
                    ErrorQueue = settings.ErrorQueue
                };

                learningTransport.HasEndpoint("Shipping");
                learningTransport.HasEndpoint("Marketing");

                rc.AddTransport(learningTransport);
            })
            .Build()
            .RunAsync().ConfigureAwait(false);
    }
}