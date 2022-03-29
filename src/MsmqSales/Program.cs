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
            Console.WriteLine("Press key to send message to LearningTransport");
            Console.ReadKey();

            await endpointInstance.Send("Shipping", new SomeCommand()).ConfigureAwait(false);
        }
    }
}