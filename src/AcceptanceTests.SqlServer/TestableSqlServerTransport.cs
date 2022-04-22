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
        var queueNames = infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Distinct();
        QueuesToCleanup.AddRange(queueNames);

        foreach (var queueName in queueNames)
        {
            var queueAddress = AcceptanceTests.SqlServer.QueueAddress.Parse(queueName);
            foreach (var sendingAddress in sendingAddresses)
            {
                if (sendingAddress == "error")
                {
                    var catalog = queueAddress.Catalog ?? "NServiceBus";
                    var schema = queueAddress.Schema ?? "dbo";
                    var errorName = sendingAddress + "@[" + schema + "]@[" + catalog + "]";
                    var bridgeError = "bridge.error" + "@[" + schema + "]@[" + catalog + "]";
                    var subscriptionRouting = "SubscriptionRouting" + "@[" + schema + "]@[" + catalog + "]";
                    QueuesToCleanup.Add(errorName);
                    QueuesToCleanup.Add(bridgeError);
                    QueuesToCleanup.Add(subscriptionRouting);
                }
            }
        }
        return infrastructure;
    }

    public static List<string> QueuesToCleanup { get; } = new List<string>();
}
