using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Transport;

public class RouterConfiguration
{
    public RouterConfiguration()
    {
    }

    public InterfaceConfiguration AddInterface(TransportDefinition transportDefinition)
    {
        var interfaceConfiguration = new InterfaceConfiguration(transportDefinition);

        interfaces.Add(interfaceConfiguration);

        return interfaceConfiguration;
    }
    public async Task<RunningRouter> Start(RouterConfiguration rc, CancellationToken cancellationToken = default)
    {
        var runningEndpoints = new List<IReceivingRawEndpoint>();

        foreach (var interfaceConfiguration in rc.interfaces)
        {
            var interfaceEndpointConfiguration = RawEndpointConfiguration.Create(
                interfaceConfiguration.Endpoint,
                interfaceConfiguration.TransportDefinition,
                (mc, d, ct) =>
                {
                    Console.WriteLine("Got message");
                    return Task.CompletedTask;
                },
                "Error");

            interfaceEndpointConfiguration.AutoCreateQueues();

            var receivingRawEndpoint = await RawEndpoint.Start(interfaceEndpointConfiguration, cancellationToken).ConfigureAwait(false);

            runningEndpoints.Add(receivingRawEndpoint);
        }

        var runningRouter = new RunningRouter(runningEndpoints);


        return runningRouter;
    }


    List<InterfaceConfiguration> interfaces = new List<InterfaceConfiguration>();
}
