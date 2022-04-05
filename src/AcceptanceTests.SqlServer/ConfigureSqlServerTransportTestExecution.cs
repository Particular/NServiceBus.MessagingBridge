using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
//using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureSqlServerTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("SqlServerTransportConnectionString") ?? @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True";
    public RouterTransportDefinition GetRouterTransport()
    {
        var transportDefinition = new TestableSqlServerTransport(connectionString);
        return new RouterTransportDefinition
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
                var connection = new SqlConnection(transport.ConnectionString);
                connection.Open();
                return connection;
            }

            return transport.ConnectionFactory(CancellationToken.None).Result;
        };

        var commandTextBuilder = new StringBuilder();

        foreach (var queue in transport.QueuesToCleanup)
        {
            commandTextBuilder.AppendLine($"IF OBJECT_ID('{queue}', 'U') IS NOT NULL DROP TABLE {queue}");
        }
        var commandText = commandTextBuilder.ToString();
        Console.WriteLine(commandText);
        if (!string.IsNullOrEmpty(commandText))
        {
            using (var conn = factory())
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    //command.ExecuteNonQuery();
                }
            };
        }

        return Task.CompletedTask;
    }
}