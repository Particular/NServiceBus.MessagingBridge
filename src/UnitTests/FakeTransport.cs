using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

partial class RouterConfigurationTests
{
    class FakeTransport : TransportDefinition
    {
        public FakeTransport() : base(TransportTransactionMode.SendsAtomicWithReceive, true, true, true)
        {
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => throw new System.NotImplementedException();
        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default) => throw new System.NotImplementedException();
#pragma warning disable CS0672 // Member overrides obsolete member
        public override string ToTransportAddress(QueueAddress address) => throw new System.NotImplementedException();
#pragma warning restore CS0672 // Member overrides obsolete member
    }
}