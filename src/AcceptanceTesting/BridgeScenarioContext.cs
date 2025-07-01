using System.Threading;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Transport;

public class BridgeScenarioContext : ScenarioContext
{
    BridgeTransport bridgeTransport;
    readonly ManualResetEventSlim transportInitializationSemaphore = new();

    public TransportDefinition TransportBeingTested
    {
        get
        {
            if (bridgeTransport is not null)
            {
                return bridgeTransport.TransportDefinition;
            }

            transportInitializationSemaphore.Wait();
            return bridgeTransport.TransportDefinition;
        }
    }

    public void Initialize(BridgeTransport transport)
    {
        bridgeTransport = transport;
        transportInitializationSemaphore.Set();
    }
}