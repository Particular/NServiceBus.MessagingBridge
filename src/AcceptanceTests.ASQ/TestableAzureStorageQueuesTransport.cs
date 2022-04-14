using NServiceBus;

public class TestableAzureStorageQueuesTransport : AzureStorageQueueTransport
{
    public TestableAzureStorageQueuesTransport(string connectionString) : base(connectionString)
    {
    }

    //public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings,
    //    ReceiveSettings[] receivers,
    //    string[] sendingAddresses,
    //    CancellationToken cancellationToken = new CancellationToken())
    //{
    //    var infrastructure = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken)
    //        .ConfigureAwait(false);
    //    QueuesToCleanup.AddRange(infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Concat(sendingAddresses)
    //        .Distinct());
    //    return infrastructure;
    //}

    //public List<string> QueuesToCleanup { get; } = new List<string>();
}