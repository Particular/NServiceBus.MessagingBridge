using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using AcceptanceTests.SqlServer;

class ConfigureSqlServerTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("SqlServerTransportConnectionString") ?? @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True";

    public BridgeTransportDefinition GetBridgeTransport()
    {
        var transportDefinition = new TestableSqlServerTransport(connectionString)
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        };
        return new BridgeTransportDefinition
        {
            TransportDefinition = transportDefinition,
            Cleanup = (ct) => Cleanup(transportDefinition, ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableSqlServerTransport(connectionString);
        endpointConfiguration.UseTransport(transportDefinition);

        return ct => Cleanup(transportDefinition, ct);
    }

    Task Cleanup(TestableSqlServerTransport transport, CancellationToken cancellationToken)
    {
        Func<SqlConnection> factory = () =>
        {
            if (transport.ConnectionString != null)
            {
                transport.TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
                var connection = new SqlConnection(transport.ConnectionString);
                connection.Open();
                return connection;
            }

            return transport.ConnectionFactory(CancellationToken.None).Result;
        };

        using (var conn = factory())
        {
            using (var command = conn.CreateCommand())
            {
                var commandTextBuilder = new StringBuilder();
                var schema = "";
                var catalog = "";
                foreach (var queue in transport.QueuesToCleanup)
                {
                    var queueAddress = QueueAddress.Parse(queue);
                    schema = queueAddress.Schema;
                    catalog = queueAddress.Catalog;
                    TryDeleteTable(conn, queueAddress);
                    TryDeleteTable(conn, new QueueAddress(queueAddress.Table + ".Delayed", schema, catalog));
                }
                TryDeleteTable(conn, new QueueAddress("SubscriptionRouting", schema, catalog));
                TryDeleteTable(conn, new QueueAddress("bridge.error", schema, catalog));
            }
        };

        return Task.CompletedTask;
    }

    static void TryDeleteTable(SqlConnection conn, QueueAddress address)
    {
        try
        {
            using (var comm = conn.CreateCommand())
            {
                comm.CommandText = $"IF OBJECT_ID('{address.QualifiedTableName}', 'U') IS NOT NULL DROP TABLE {address.QualifiedTableName}";
                comm.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("it does not exist or you do not have permission"))
            {
                throw;
            }
        }
    }
}