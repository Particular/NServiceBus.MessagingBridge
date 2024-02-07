namespace NServiceBus.MessagingBridge.Heartbeats;

using System;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using NServiceBus.Logging;
using ServiceControl.Plugin.Heartbeat.Messages;
using Transport;

class HeartbeatSender(IMessageDispatcher dispatcher, HostInformation hostInfo,
    ServiceControlBackend backend, string endpointName, TimeSpan interval, TimeSpan timeToLive) : IDisposable
{
    async Task SendHeartbeatsAndSwallowExceptions(CancellationToken cancellationToken)
    {
        Logger.Debug($"Start sending heartbeats every {interval}");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

                var message = new EndpointHeartbeat
                {
                    ExecutedAt = DateTime.UtcNow,
                    EndpointName = endpointName,
                    Host = hostInfo.DisplayName,
                    HostId = hostInfo.HostId
                };

                await backend.Send(message, timeToLive, dispatcher, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // private token, sender is being stopped, log the exception in case the stack trace is ever needed for debugging
                Logger.Debug("Operation canceled while stopping heartbeat sending.", ex);
                break;
            }
            catch (Exception ex)
            {
                Logger.Warn("Unable to send heartbeat to ServiceControl.", ex);
            }
        }
    }

    public Task Start(CancellationToken cancellationToken = default)
    {
        stoppingCancelationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        sendHeartBeatsTask = SendHeartbeatsAndSwallowExceptions(stoppingCancelationTokenSource.Token);

        if (sendHeartBeatsTask.IsCompleted)
        {
            return sendHeartBeatsTask;
        }

        return Task.CompletedTask;
    }

    public Task Stop(CancellationToken cancellationToken = default)
    {
        stoppingCancelationTokenSource!.Cancel();

        return Task.CompletedTask;
    }

    public void Dispose() => stoppingCancelationTokenSource.Cancel();

    CancellationTokenSource stoppingCancelationTokenSource;
    Task sendHeartBeatsTask;
    static readonly ILog Logger = LogManager.GetLogger<HeartbeatSender>();


}