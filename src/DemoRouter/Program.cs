using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var rc = new MessageRouterConfiguration();

        rc.AddInterface(new MsmqTransport())
            .HasEndpoint("Sales").AtMachine("ServerA")
            .HasEndpoint("Finance").AtMachine("ServerB");

        // Note to Kyle & Travis, the above code doesn't work yet. The `AtMachine` I just made up.
        // Would it be possible to only have AtMachine available when you're on MsqmTransport?
        
        rc.AddInterface(new LearningTransport())
            .HasEndpoint("Shipping")
            .HasEndpoint("Marketing");

        // rc.AddInterface(new SqlTransport())
        //     .HasEndpoint("OneMore")
        //     .HasEndpoint("OneMoreMore");

        var runningRouter = await rc.Start(rc).ConfigureAwait(false);

        Console.ReadKey();

        await runningRouter.Stop().ConfigureAwait(false);


    }
}