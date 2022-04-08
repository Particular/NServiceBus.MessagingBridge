using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class FakeTransport : TransportDefinition
{
    public FakeTransport() : base(TransportTransactionMode.TransactionScope, true, true, true)
    {
    }
    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
              new[]
              {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive,
                TransportTransactionMode.TransactionScope,
              };

    public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default) => throw new System.NotImplementedException();
#pragma warning disable CS0672 // Member overrides obsolete member
    public override string ToTransportAddress(QueueAddress address) => throw new System.NotImplementedException();
#pragma warning restore CS0672 // Member overrides obsolete member
}