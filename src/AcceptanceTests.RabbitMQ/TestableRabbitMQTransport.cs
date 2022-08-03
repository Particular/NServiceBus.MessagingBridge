using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class TestableRabbitMQTransport : RabbitMQTransport
{
    public TestableRabbitMQTransport(RoutingTopology topology, string connectionString)
        : base(topology, connectionString)
    {
    }

    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        var infrastructure = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken).ConfigureAwait(false);
        QueuesToCleanup.AddRange(infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Concat(sendingAddresses).Distinct());
        return infrastructure;
    }

    public List<string> QueuesToCleanup { get; } = new List<string>();
}
