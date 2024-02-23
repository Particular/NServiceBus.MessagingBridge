namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Logging;
    using ServiceControl.Plugin.Heartbeat.Messages;
    using Transport;

    class HeartbeatSender : IDisposable
    {
        async Task SendHeartbeatsAndSwallowExceptions(CancellationToken cancellationToken)
        {
            Logger.Debug($"Start sending heartbeats every {_interval}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);

                    var message = new EndpointHeartbeat
                    {
                        ExecutedAt = DateTime.UtcNow,
                        EndpointName = _endpointName,
                        Host = _hostInfo.DisplayName,
                        HostId = _hostInfo.HostId
                    };

                    await _backend.Send(message, _timeToLive, _dispatcher, cancellationToken).ConfigureAwait(false);
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

        async Task SendEndpointStartupMessageAndSwallowExceptions(DateTime startupTime, TimeSpan delay, bool retry, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Debug($"Send endpoint startup message");

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                var message = new RegisterEndpointStartup
                {
                    HostId = _hostInfo.HostId,
                    Host = _hostInfo.DisplayName,
                    Endpoint = _endpointName,
                    HostDisplayName = _hostInfo.DisplayName,
                    HostProperties = _hostInfo.Properties,
                    StartedAt = startupTime,
                };

                await _backend.Send(message, _timeToLive, _dispatcher, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // private token, check is being stopped, log the exception in case the stack trace is ever needed for debugging
                Logger.Debug("Operation canceled while stopping heartbeat sending.", ex);
                return;
            }
            catch (Exception ex)
            {
                if (retry)
                {
                    Logger.Warn($"Unable to register endpoint startup with ServiceControl. Going to reattempt registration after {registrationRetryInterval}.", ex);
                    await SendEndpointStartupMessageAndSwallowExceptions(startupTime, registrationRetryInterval, false, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Logger.Warn("Unable to register endpoint startup with ServiceControl.", ex);
                }
            }
        }

        public Task Start(CancellationToken cancellationToken = default)
        {
            stoppingCancelationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = SendEndpointStartupMessageAndSwallowExceptions(DateTime.UtcNow, default, true, stoppingCancelationTokenSource.Token);

            _ = SendHeartbeatsAndSwallowExceptions(stoppingCancelationTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task Stop(CancellationToken cancellationToken = default)
        {
            stoppingCancelationTokenSource.Cancel();

            return Task.CompletedTask;
        }

        public void Dispose() => stoppingCancelationTokenSource.Cancel();

        CancellationTokenSource stoppingCancelationTokenSource;

        static readonly TimeSpan registrationRetryInterval = TimeSpan.FromMinutes(1);
        static readonly ILog Logger = LogManager.GetLogger<HeartbeatSender>();
        readonly IMessageDispatcher _dispatcher;
        readonly HostInformation _hostInfo;
        readonly HeartbeatServiceControlBackend _backend;
        readonly string _endpointName;
        readonly TimeSpan _interval;
        readonly TimeSpan _timeToLive;

        public HeartbeatSender(IMessageDispatcher dispatcher,
            HostInformation hostInfo,
            HeartbeatServiceControlBackend backend,
            string endpointName,
            TimeSpan interval,
            TimeSpan timeToLive)
        {
            _dispatcher = dispatcher;
            _hostInfo = hostInfo;
            _backend = backend;
            _endpointName = endpointName;
            _interval = interval;
            _timeToLive = timeToLive;
        }
    }
}