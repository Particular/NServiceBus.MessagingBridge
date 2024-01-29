namespace NServiceBus.MessagingBridge.Heartbeats;

using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System;
using System.Threading;
using Microsoft.Extensions.Hosting;
using NServiceBus.Transport;
using ServiceControl.Plugin.Heartbeat.Messages;
using System.Threading.Tasks;
using NServiceBus.Routing;
using System.Linq;
using NServiceBus.Raw;

class HeartbeatHostedService(FinalizedBridgeConfiguration bridgeConfiguration, EndpointRegistry endpointRegistry) : IHostedService
{
    CancellationTokenSource cancellationTokenSource = new();
    static readonly Guid HostId = new("{E0CBAF80-E833-42D4-882D-6B5E198C29E8}");

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        var transportsWithHeartbeats = bridgeConfiguration.TransportConfigurations
            .Where(t => t.HeartbeatConfiguration != null).ToDictionary(c => c.Name);

        var transports = endpointRegistry.Registrations
            .Where(r => transportsWithHeartbeats.ContainsKey(r.TranportName))
            .Select(r => new { r.TranportName, r.RawEndpoint }).DistinctBy(r => r.TranportName);

        foreach (var transport in transports)
        {
            _ = StartHeartbeat(transport.RawEndpoint, transportsWithHeartbeats[transport.TranportName].HeartbeatConfiguration, cancellationTokenSource.Token);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationTokenSource.Cancel();

        return Task.CompletedTask;
    }

    static async Task StartHeartbeat(IStartableRawEndpoint endpoint, HeartbeatConfiguration heartbeatConfig, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var endpointHeartbeat = new EndpointHeartbeat()
            {
                EndpointName = "MessagingBridge",
                ExecutedAt = DateTime.UtcNow,
                Host = Environment.MachineName,
                HostId = HostId
            };

            var headers = new Dictionary<string, string>()
            {
                [Headers.EnclosedMessageTypes] = endpointHeartbeat.GetType().FullName,
                [Headers.ContentType] = ContentTypes.Json,
                [Headers.MessageIntent] = "Send"
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(endpointHeartbeat));

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, messageBody);
            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(heartbeatConfig.ServiceControlQueue)));

            try
            {
                await endpoint.Dispatch(transportOperations, new TransportTransaction(), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await Task.Delay((int)heartbeatConfig.Frequency.TotalMilliseconds, cancellationToken).ConfigureAwait(true);
        }
    }


}
