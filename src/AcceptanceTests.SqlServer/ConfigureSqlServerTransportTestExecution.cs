using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using AcceptanceTests.SqlServer;

class ConfigureSqlServerTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("SqlServerTransportConnectionString");
    TestableSqlServerTransport transportDefinition;

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        transportDefinition = new TestableSqlServerTransport(connectionString);
        endpointConfiguration.UseTransport(transportDefinition);

        return Task.CompletedTask;
    }

    public Task Cleanup() => Cleanup(transportDefinition, CancellationToken.None);


    public BridgeTransport Configure(PublisherMetadata publisherMetadata) =>
        new TestableSqlServerTransport(connectionString)
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        }.ToTestableBridge();

    public Task Cleanup(BridgeTransport bridgeTransport) => Cleanup(bridgeTransport.FromTestableBridge<TestableSqlServerTransport>(), CancellationToken.None);

    async Task Cleanup(TestableSqlServerTransport transport, CancellationToken cancellationToken)
    {
        await using var conn = await ConnectionFactory(transport, cancellationToken);
        await using var command = conn.CreateCommand();
        var schema = "";
        var catalog = "";
        foreach (var queue in transport.QueuesToCleanup)
        {
            var queueAddress = QueueAddress.Parse(queue);
            schema = queueAddress.Schema;
            catalog = queueAddress.Catalog;
            await TryDeleteTable(conn, queueAddress);
            await TryDeleteTable(conn, new QueueAddress(queueAddress.Table + ".Delayed", schema, catalog));
        }
        await TryDeleteTable(conn, new QueueAddress("SubscriptionRouting", schema, catalog));
        await TryDeleteTable(conn, new QueueAddress("bridge.error", schema, catalog));
        return;

        static async Task<SqlConnection> ConnectionFactory(TestableSqlServerTransport transport, CancellationToken cancellationToken)
        {
            if (transport.ConnectionString == null)
            {
                return await transport.ConnectionFactory(cancellationToken);
            }

            transport.TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
            var connection = new SqlConnection(transport.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    static async Task TryDeleteTable(SqlConnection conn, QueueAddress address)
    {
        try
        {
            await using var comm = conn.CreateCommand();
            comm.CommandText = $"IF OBJECT_ID('{address.QualifiedTableName}', 'U') IS NOT NULL DROP TABLE {address.QualifiedTableName}";
            await comm.ExecuteNonQueryAsync();
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
