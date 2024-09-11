using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Raw;

class DispatcherFactory
{
    public static Task<IRawDispatcher> CreateDispatcher(BridgeTransport transportConfiguration, CancellationToken cancellationToken = default)
    {
        var dispatcher = new InitializableRawDispatcher(transportConfiguration);
        return dispatcher.Initialize(cancellationToken);
    }
}