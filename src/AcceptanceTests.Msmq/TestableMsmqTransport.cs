using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

/// <summary>
/// A dedicated subclass of the MsmqTransport that enables us to intercept the receive queues for the test.
/// </summary>
class TestableMsmqTransport : MsmqTransport
{
    public string[] ReceiveQueues = Array.Empty<string>();

    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        MessageEnumeratorTimeout = TimeSpan.FromMilliseconds(10);

        var infrastructure = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken).ConfigureAwait(false);
        ReceiveQueues = infrastructure.Receivers.Select(r => r.Value.ReceiveAddress).ToArray();

        return infrastructure;
    }
}