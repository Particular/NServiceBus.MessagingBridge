namespace NServiceBus.Raw;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

class InitializableRawDispatcher(BridgeTransport transportConfiguration)
{
    readonly BridgeTransport transportConfiguration = transportConfiguration ?? throw new ArgumentNullException(nameof(transportConfiguration));

    public async Task<IRawDispatcher> Initialize(CancellationToken cancellationToken = default)
    {
        var dispatcherName = $"bridge-dispatcher-{transportConfiguration.Name}";
        var hostSettings = new HostSettings(
            dispatcherName,
            $"Host for {dispatcherName}",
            new StartupDiagnosticEntries(),
            (_, _, _) => { },
            false);

        var transportInfrastructure = await transportConfiguration.TransportDefinition.Initialize(hostSettings, [], [], cancellationToken).ConfigureAwait(false);

        return new RawDispatcher(transportInfrastructure);
    }
}