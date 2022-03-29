using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

public class MessageRouterConfiguration
{
    public MessageRouterConfiguration()
    {
    }

    public InterfaceConfiguration AddInterface(TransportDefinition transportDefinition)
    {
        var interfaceConfiguration = new InterfaceConfiguration(transportDefinition);
        interfaces.Add(interfaceConfiguration);

        return interfaceConfiguration;
    }

    public async Task<RunningRouter> Start(MessageRouterConfiguration rc, CancellationToken cancellationToken = default)
    {
        // Loop through all interfaces
        foreach (var interfaceConfiguration in interfaces)
        {
            // Get all endpoint-names that I need to host
            // That is all endpoint-names that I don't have on this interface.
            var endpoints = interfaces.Where(s => s != interfaceConfiguration).SelectMany(s => s.Endpoints);

            // What we need is an object where all endpoints are in for a specific transport
            // Use that endpoint to send a message. It really doesn't care which one of them.
            // For MSMQ, we have 1 object with references to the two running endpoints we create.
            // For LearningTransport, we also have 1 object with references to the two running endpoints.

            foreach (var endpointToSimulate in endpoints)
            {
                var interfaceEndpointConfiguration = RawEndpointConfiguration.Create(
                    endpointToSimulate,
                    interfaceConfiguration.TransportDefinition,
                    (mt, _, ct) => MoveMessage(endpointToSimulate, mt, ct),
                    "error");

                interfaceEndpointConfiguration.AutoCreateQueues();
                interfaceEndpointConfiguration.LimitMessageProcessingConcurrencyTo(1);

                // Create the actual endpoint
                var runningRawEndpoint = await RawEndpoint.Start(interfaceEndpointConfiguration, cancellationToken)
                    .ConfigureAwait(false);

                // Find the interface that has my TransportDefinition and attach it
                interfaces.Single(s => s.TransportDefinition == interfaceConfiguration.TransportDefinition)
                    .RunningEndpoint = runningRawEndpoint;

                runningEndpoints.Add(runningRawEndpoint);
            }
        }

        return new RunningRouter(runningEndpoints);
    }

    async Task MoveMessage(string endpointName, MessageContext messageContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("Moving the message over");

        // Find dispatchers that do NOT have this endpoint
        // We have 4 in this demo
        // MSMQ-Marketing
        // MSMQ-Shipping
        // Learning-Billing <-- We need this one, but this is running on MSMQ runningendpoint. We need the other one!
        // Learning-Sales
        // var rawEndpoint = runningEndpoints.Single(e => e.EndpointName == endpointName);
        var rawEndpoint = interfaces.Single(s => s.Endpoints.Contains(endpointName)).RunningEndpoint;

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers,
            messageContext.Body);

        var address = rawEndpoint.ToTransportAddress(new QueueAddress(endpointName));
        var tranportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(address));
        await rawEndpoint.Dispatch(new TransportOperations(tranportOperation), new TransportTransaction(),
            cancellationToken).ConfigureAwait(false);
    }

    List<IReceivingRawEndpoint> runningEndpoints = new List<IReceivingRawEndpoint>();
    List<InterfaceConfiguration> interfaces = new List<InterfaceConfiguration>();
}
