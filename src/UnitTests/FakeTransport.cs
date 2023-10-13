using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class FakeTransport : TransportDefinition
{
    public FakeTransport(TransportTransactionMode transportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive)
        : base(transportTransactionMode, true, true, true)
    {
        foreach (TransportTransactionMode enumValue in Enum.GetValues(typeof(TransportTransactionMode)))
        {
            if (enumValue <= transportTransactionMode)
            {
                supportedTransactionModes.Add(enumValue);
            }
        }
    }

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => supportedTransactionModes;

    public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    List<TransportTransactionMode> supportedTransactionModes = [];
}