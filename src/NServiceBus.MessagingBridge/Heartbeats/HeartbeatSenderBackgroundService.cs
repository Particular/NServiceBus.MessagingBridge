namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Microsoft.Extensions.Hosting;
    using Raw;
    using Transport;
    using Utils;

    class HeartbeatSenderBackgroundService : BackgroundService
    {
        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            foreach (var heartbeatSender in heartbeatSenders)
            {
                await heartbeatSender.Stop(cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var transportConfiguration in _finalizedBridgeConfiguration.TransportConfigurations)
            {
                if (transportConfiguration.Heartbeats.ServiceControlQueue != null)
                {
                    var heartbeatSender =
                        await ConfigureHeartbeatSenderForTransport(transportConfiguration, cancellationToken)
                            .ConfigureAwait(false);

                    await heartbeatSender.Start(cancellationToken).ConfigureAwait(false);

                    heartbeatSenders.Add(heartbeatSender);
                }
            }
        }

        static async Task<IMessageDispatcher> CreateSendOnlyMessageDispatcher(
            BridgeTransport bridgeTransportConfiguration,
            CancellationToken cancellationToken)
        {
            var endpointName = $"MessagingBridge.{bridgeTransportConfiguration.Name}"; // MessageBridge.TransportName

            var criticalError = new RawCriticalError(null);

            var hostSettings = new HostSettings(
                endpointName,
                $"NServiceBus.Raw host for {endpointName}",
                new StartupDiagnosticEntries(),
                criticalError.Raise,
                false,
                null);

            var transportInfrastructure =
                await bridgeTransportConfiguration.TransportDefinition.Initialize(
                        hostSettings,
                        Array.Empty<ReceiveSettings>(),
                        Array.Empty<string>(),
                        cancellationToken)
                    .ConfigureAwait(false);

            return transportInfrastructure.Dispatcher;
        }

        static HostInformation GetHostInformation()
        {
            var displayName = Environment.MachineName;
            var hostId = DeterministicGuid.Create(displayName, PathUtilities.SanitizedPath(Environment.CommandLine));

            return new HostInformation(hostId, displayName);
        }

        static async Task<HeartbeatSender> ConfigureHeartbeatSenderForTransport(
            BridgeTransport bridgeTransportConfiguration,
            CancellationToken cancellationToken)
        {
            //receiveAddress is null because the heartbeat endpoint is send only
            var serviceControlBackEnd = new HeartbeatServiceControlBackend(
                bridgeTransportConfiguration.Heartbeats.ServiceControlQueue,
                receiveAddresses: null);

            var sendOnlyMessageDispatcher =
                await CreateSendOnlyMessageDispatcher(bridgeTransportConfiguration, cancellationToken)
                    .ConfigureAwait(false);

            var heartBeatSender = new HeartbeatSender(
                sendOnlyMessageDispatcher,
                GetHostInformation(),
                serviceControlBackEnd,
                $"MessagingBridge.{bridgeTransportConfiguration.Name}",
                bridgeTransportConfiguration.Heartbeats.Frequency,
                bridgeTransportConfiguration.Heartbeats.TimeToLive);

            return heartBeatSender;
        }

        readonly List<HeartbeatSender> heartbeatSenders;
        readonly FinalizedBridgeConfiguration _finalizedBridgeConfiguration;

        public HeartbeatSenderBackgroundService(FinalizedBridgeConfiguration finalizedBridgeConfiguration)
        {
            _finalizedBridgeConfiguration = finalizedBridgeConfiguration;
            heartbeatSenders = new List<HeartbeatSender>();
        }
    }
}