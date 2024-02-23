namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using Microsoft.Extensions.Hosting;
using Raw;
using ServiceControl.Plugin.CustomChecks.Messages;
using Transport;

[SuppressMessage("Code",
    "PS0003:A parameter of type CancellationToken on a non-private delegate or method should be optional")]
class CustomChecksBackgroundService
    : BackgroundService
{
    public CustomChecksBackgroundService(
        FinalizedBridgeConfiguration bridgeConfiguration,
        IEnumerable<ICustomCheck> customChecks)
    {
        this.bridgeConfiguration = bridgeConfiguration;
        this.customChecks = customChecks.ToList();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!customChecks.Any())
        {
            return;
        }

        await Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray()).ConfigureAwait(false);
    }

#pragma warning disable PS0017
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
#pragma warning restore PS0017
    {
        if (!customChecks.Any())
        {
            return;
        }

        var hostInfo = GetHostInformation();

        var sendOnlyMessageDispatchers = new List<Func<ICustomCheck, CheckResult, CancellationToken, Task>>();

        foreach (var bridgeTransportConfiguration in bridgeConfiguration.TransportConfigurations)
        {
            if (bridgeTransportConfiguration.CustomChecks != null)
            {
                var sendOnlyMessageDispatcher =
                    await CreateSendOnlyMessageDispatcher(bridgeTransportConfiguration, stoppingToken)
                        .ConfigureAwait(false);

                var serviceControlBackend =
                    new CustomCheckServiceControlBackend(
                        bridgeTransportConfiguration.CustomChecks.ServiceControlQueue,
                        sendOnlyMessageDispatcher);

                sendOnlyMessageDispatchers.Add(async (check, checkResult, cancellationToken) =>
                {
                    var message = new ReportCustomCheckResult
                    {
                        CustomCheckId = check.Id,
                        Category = check.Category,
                        HasFailed = checkResult.HasFailed,
                        FailureReason = checkResult.FailureReason,
                        ReportedAt = DateTime.UtcNow,
                        EndpointName = $"MessagingBridge.{bridgeTransportConfiguration.Name}",
                        Host = hostInfo.DisplayName,
                        HostId = hostInfo.HostId
                    };

                    var timeToLive = bridgeTransportConfiguration.CustomChecks.TimeToLive;

                    if (timeToLive == null && check.Interval.HasValue)
                    {
                        timeToLive = check.Interval.Value * 4;
                    }

                    await serviceControlBackend.Send(message, timeToLive, cancellationToken).ConfigureAwait(false);
                });
            }
        }

        timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(customChecks.Count);

        foreach (var customCheck in customChecks)
        {
            var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(
                customCheck,
                sendOnlyMessageDispatchers);

            timerBasedPeriodicCheck.Start();

            timerPeriodicChecks.Add(timerBasedPeriodicCheck);
        }
    }

    static HostInformation GetHostInformation()
    {
        var displayName = Environment.MachineName;
        var hostId = DeterministicGuid.Create(displayName, PathUtilities.SanitizedPath(Environment.CommandLine));

        return new HostInformation(hostId, displayName);
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

    FinalizedBridgeConfiguration bridgeConfiguration;
    List<ICustomCheck> customChecks;
    List<TimerBasedPeriodicCheck> timerPeriodicChecks;
}