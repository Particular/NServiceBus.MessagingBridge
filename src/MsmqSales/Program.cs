using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var endpointConfiguration = new EndpointConfiguration("Sales");
        endpointConfiguration.UsePersistence<MsmqPersistence>();
        endpointConfiguration.UseTransport(new MsmqTransport());
        endpointConfiguration.SendFailedMessagesTo("Error");
        endpointConfiguration.EnableInstallers();

        var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

        Console.WriteLine("Press a key to send");
        Console.ReadKey();

        await endpointInstance.Send("Billing", new SomeCommand()).ConfigureAwait(false);


        Console.ReadKey();

    }
}