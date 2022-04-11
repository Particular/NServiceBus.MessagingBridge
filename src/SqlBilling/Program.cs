class Program
{
    static async Task Main()
    {
        var endpointConfiguration = new EndpointConfiguration("Billing");
        var connectionstring = "Data Source=;Initial Catalog=;Integrated Security=True";
        endpointConfiguration.UseTransport(new SqlServerTransport(connectionstring));
        endpointConfiguration.UsePersistence<NonDurablePersistence>();

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
        recoverability.Delayed(delayed => delayed.NumberOfRetries(0));

        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.AuditProcessedMessagesTo("audit");
        endpointConfiguration.EnableInstallers();

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);

        while (true)
        {
            Console.WriteLine("\nPress '1' to send message to Sales on MSMQ");
            Console.WriteLine("Press '2' to send message to Shipping on Learning");
            var keypress = Console.ReadKey();

#pragma warning disable IDE0010 // Add missing cases
            switch (keypress.Key)
            {
                case ConsoleKey.D1:
                    await endpointInstance.Send("Sales", new SomeCommand()).ConfigureAwait(false);
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