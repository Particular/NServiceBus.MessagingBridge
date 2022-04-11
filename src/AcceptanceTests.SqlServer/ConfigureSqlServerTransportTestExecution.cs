using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

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

    class QueueAddress
    {
        public QueueAddress(string table, string schemaName, string catalogName)
        {
            Table = table;
            Catalog = SafeUnquote(catalogName);
            Schema = SafeUnquote(schemaName);
        }

        public string Catalog { get; }
        public string Table { get; }
        public string Schema { get; }

        public string QualifiedTableName => $"{Quote(Catalog)}.{Quote(Schema)}.{Quote(Table)}";

        public static QueueAddress Parse(string address)
        {
            var firstAtIndex = address.IndexOf("@", StringComparison.Ordinal);

            if (firstAtIndex == -1)
            {
                return new QueueAddress(address, null, null);
            }

            var tableName = address.Substring(0, firstAtIndex);
            address = firstAtIndex + 1 < address.Length ? address.Substring(firstAtIndex + 1) : string.Empty;

            address = ExtractNextPart(address, out var schemaName);

            string catalogName = null;

            if (address != string.Empty)
            {
                ExtractNextPart(address, out catalogName);
            }

            return new QueueAddress(tableName, schemaName, catalogName);
        }

        static string ExtractNextPart(string address, out string part)
        {
            var noRightBrackets = 0;
            var index = 1;

            while (true)
            {
                if (index >= address.Length)
                {
                    part = address;
                    return string.Empty;
                }

                if (address[index] == '@' && (address[0] != '[' || noRightBrackets % 2 == 1))
                {
                    part = address.Substring(0, index);
                    return index + 1 < address.Length ? address.Substring(index + 1) : string.Empty;
                }

                if (address[index] == ']')
                {
                    noRightBrackets++;
                }

                index++;
            }
        }

        static string Quote(string name)
        {
            if (name == null)
            {
                return null;
            }

            return prefix + name.Replace(suffix, suffix + suffix) + suffix;
        }

        static string SafeUnquote(string name)
        {
            var result = Unquote(name);
            return string.IsNullOrWhiteSpace(result)
                ? null
                : result;
        }

        static string Unquote(string quotedString)
        {
            if (quotedString == null)
            {
                return null;
            }

            if (!quotedString.StartsWith(prefix) || !quotedString.EndsWith(suffix))
            {
                return quotedString;
            }

            return quotedString
                .Substring(prefix.Length, quotedString.Length - prefix.Length - suffix.Length).Replace(suffix + suffix, suffix);
        }

        const string prefix = "[";
        const string suffix = "]";
    }
}