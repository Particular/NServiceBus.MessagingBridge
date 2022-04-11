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

        while (true)
        {
            Console.WriteLine("\nPress '1' to send message to Billing on SQL");
            Console.WriteLine("Press '2' to send message to Shipping on Learning");
            var keypress = Console.ReadKey();

#pragma warning disable IDE0010 // Add missing cases
            switch (keypress.Key)
            {
                case ConsoleKey.D1:
                    await endpointInstance.Send("Billing", new SomeCommand()).ConfigureAwait(false);
                    continue;
                case ConsoleKey.D2:
                    await endpointInstance.Send("Shipping", new SomeCommand()).ConfigureAwait(false);
                    continue;
                default:
                    return;
            }
#pragma warning restore IDE0010 // Add missing cases
        }
    }
}