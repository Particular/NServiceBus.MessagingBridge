using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

public class TestableAzureServiceBusTransport : AzureServiceBusTransport
{
    public TestableAzureServiceBusTransport(string? connectionString) : base(connectionString)
    {
    }

    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings,
        ReceiveSettings[] receivers,
        string[] sendingAddresses,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var infrastructure = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken)
            .ConfigureAwait(false);
        QueuesToCleanup.AddRange(infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Concat(sendingAddresses)
            .Distinct());
        return infrastructure;
    }

    public List<string> QueuesToCleanup { get; } = new List<string>();
}