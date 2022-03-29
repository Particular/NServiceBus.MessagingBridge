using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var rc = new MessageRouterConfiguration();

        rc.AddInterface(new MsmqTransport())
            .HasEndpoint("Sales")
            .HasEndpoint("Finance");

        rc.AddInterface(new LearningTransport())
            .HasEndpoint("Shipping")
            .HasEndpoint("Marketing");

        // rc.AddInterface(new SqlTransport())
        //     .HasEndpoint("OneMore")
        //     .HasEndpoint("OneMoreMore");

        // Let's take "Sales"
        // Endpoints need to be create for "Sales" on both LearningTransport and SqlTransport
        // But when a message arrives for "Sales", I need to forward that to MsmqTransport, of which I have no knowledge.
        //
        // So I need an object that says
        // RunningEndpoint=MSMQ
        // EndpointsThatCanBeFindOnMySide = Sales, Finance
        // And there are four freaking endpoints running on MSMQ, but I only need one of these:
        // - Shipping
        // - Marketing
        // - OneMore
        // - OneMoreMore

        var runningRouter = await rc.Start(rc).ConfigureAwait(false);

        Console.ReadKey();

        await runningRouter.Stop().ConfigureAwait(false);


    }
}