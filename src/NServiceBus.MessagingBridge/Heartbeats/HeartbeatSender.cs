namespace NServiceBus.MessagingBridge.Heartbeats;

using System;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using ServiceControl.Plugin.Heartbeat.Messages;
using Transport;

/// <summary>
/// 
/// </summary>
public class HeartbeatSender : IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dispatcher"></param>
    /// <param name="hostInfo"></param>
    /// <param name="backend"></param>
    /// <param name="endpointName"></param>
    /// <param name="interval"></param>
    /// <param name="timeToLive"></param>
    public HeartbeatSender(IMessageDispatcher dispatcher, HostInformation hostInfo,
        ServiceControlBackend backend, string endpointName, TimeSpan interval, TimeSpan timeToLive)
    {
        this.dispatcher = dispatcher;
        this.hostInfo = hostInfo;
        this.backend = backend;
        this.endpointName = endpointName;
        this.interval = interval;
        this.timeToLive = timeToLive;
    }

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Start(CancellationToken cancellationToken)
    {
        stoppingCancelationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        sendHeartBeatsTask = SendHeartbeatsAndSwallowExceptions(stoppingCancelationTokenSource.Token);

        if (sendHeartBeatsTask.IsCompleted)
        {
            return sendHeartBeatsTask;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task Stop(CancellationToken cancellationToken)
    {
        stoppingCancelationTokenSource!.Cancel();
    }

    CancellationTokenSource stoppingCancelationTokenSource;
    Task sendHeartBeatsTask;
    readonly IMessageDispatcher dispatcher;
    readonly HostInformation hostInfo;
    readonly ServiceControlBackend backend;
    readonly TimeSpan interval;
    readonly TimeSpan timeToLive;
    readonly string endpointName;

    /// <summary>
    /// 
    /// </summary>
    public void Dispose() => stoppingCancelationTokenSource.Cancel();
}