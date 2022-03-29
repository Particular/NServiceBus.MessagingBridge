using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Routing;
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
        var allEndpoints = rc.interfaces.Select(i => i.Endpoint).Distinct().ToArray();

        foreach (var interfaceConfiguration in rc.interfaces)
        {
            var endpointToSimulate = allEndpoints.Single(i => i != interfaceConfiguration.Endpoint);

            var interfaceEndpointConfiguration = RawEndpointConfiguration.Create(
                endpointToSimulate,
                interfaceConfiguration.TransportDefinition,
                (mt, _, ct) => MoveMessage(endpointToSimulate, mt, ct),
                "Error");

            interfaceEndpointConfiguration.AutoCreateQueues(allEndpoints);

            var receivingRawEndpoint = await RawEndpoint.Start(interfaceEndpointConfiguration, cancellationToken).ConfigureAwait(false);

            runningEndpoints.Add(receivingRawEndpoint);
        }

        var runningRouter = new RunningRouter(runningEndpoints);


        return runningRouter;
    }

    async Task MoveMessage(string endpointName, MessageContext messageContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("Moving the message over");

        var dispatchers = runningEndpoints.Where(e => e.EndpointName != endpointName);

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        foreach (var dispatcher in dispatchers)
        {
            var address = dispatcher.ToTransportAddress(new QueueAddress(endpointName));
            var tranportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(address));
            await dispatcher.Dispatch(new TransportOperations(tranportOperation), new TransportTransaction(), cancellationToken).ConfigureAwait(false);
        }

    }

    List<IReceivingRawEndpoint> runningEndpoints = new List<IReceivingRawEndpoint>();
    List<InterfaceConfiguration> interfaces = new List<InterfaceConfiguration>();
}
