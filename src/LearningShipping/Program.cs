class Program
{
    static async Task Main()
    {
        var endpointConfiguration = new EndpointConfiguration("Shipping");
        endpointConfiguration.UseTransport(new LearningTransport());

        var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

        while (true)
        {
            Console.WriteLine("Press key to send message to Sales on MSMQ");
            Console.ReadKey();

            await endpointInstance.Send("Sales", new SomeCommand()).ConfigureAwait(false);
        }

    }
}