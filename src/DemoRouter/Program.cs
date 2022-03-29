using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var rc = new RouterConfiguration();

        rc.AddInterface(new MsmqTransport())
            .HasEndpoint("Sales");
        rc.AddInterface(new LearningTransport())
            .HasEndpoint("Billing");

        var runningRouter = await rc.Start(rc).ConfigureAwait(false);

        Console.ReadKey();

        await runningRouter.Stop().ConfigureAwait(false);


    }
}