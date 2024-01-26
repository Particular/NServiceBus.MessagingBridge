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

class HeartbeatHostedService(IEndpointRegistry endpointRegistry) : IHostedService
{
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    static readonly Guid HostId = new Guid("{E0CBAF80-E833-42D4-882D-6B5E198C29E8}");

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {

                var targetEndpointDispatcher = endpointRegistry.GetTargetEndpointDispatcher("N3");
                var endpointHeartbeat = new EndpointHeartbeat()
                {
                    EndpointName = "MessagingBridge",
                    ExecutedAt = DateTime.UtcNow,
                    Host = "Laptop2",
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

                await targetEndpointDispatcher.Dispatch(outgoingMessage, new TransportTransaction(), cancellationToken)
                    .ConfigureAwait(false);

                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(true);
            }
        }, cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationTokenSource.Cancel();

        return Task.CompletedTask;
    }
}
