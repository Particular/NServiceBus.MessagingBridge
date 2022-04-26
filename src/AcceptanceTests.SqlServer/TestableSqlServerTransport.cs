using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class TestableSqlServerTransport : SqlServerTransport
{
    public TestableSqlServerTransport(string connectionString)
        : base(connectionString)
    {
    }

    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        var infrastructure = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken).ConfigureAwait(false);
        //var queueNames = infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Distinct();
        //foreach (var queueName in queueNames)
        //{
        //    var queueAddress = AcceptanceTests.SqlServer.QueueAddress.Parse(queueName);
        //    foreach (var sendingAddress in sendingAddresses)
        //    {
        //        if (sendingAddress == "error")
        //        {
        //            var errorName = sendingAddress + "@[" + queueAddress.Schema + "]@[" + queueAddress.Catalog + "]";
        //            QueuesToCleanup.Add(errorName);
        //        }
        //    }
        //}
        //QueuesToCleanup.AddRange(queueNames.Concat(sendingAddresses).Distinct());

        //QueuesToCleanup.AddRange(infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Concat(sendingAddresses).Distinct());
        return infrastructure;
    }

    public List<string> QueuesToCleanup { get; } = new List<string>();
}
