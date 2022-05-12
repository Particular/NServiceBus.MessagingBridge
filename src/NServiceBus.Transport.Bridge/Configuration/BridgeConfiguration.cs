namespace NServiceBus
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using NServiceBus.Transport;

    /// <summary>
    /// Configuration options for bridging multiple transports
    /// </summary>
    public class BridgeConfiguration
    {
        /// <summary>
        /// Configures the bridge with the given transport
        /// </summary>
        public void AddTransport(BridgeTransport transportConfiguration)
        {
            if (transportConfigurations.Any(t => t.Name == transportConfiguration.Name))
            {
                throw new InvalidOperationException($"A transport with the name {transportConfiguration.Name} has already been configured. Use a different transport type or specify a custom name");
            }

            transportConfigurations.Add(transportConfiguration);
        }

        /// <summary>
        /// Runs the bridge in receive-only transaction mode regardless of whether the bridged transports support distributed transactions
        /// </summary>
        public void RunInReceiveOnlyTransactionMode()
        {
            runInReceiveOnlyTransactionMode = true;
        }

        internal FinalizedBridgeConfiguration FinalizeConfiguration(ILogger<BridgeConfiguration> logger)
        {
            if (transportConfigurations.Count < 2)
            {
                throw new InvalidOperationException("At least two transports needs to be configured");
            }

            var tranportsWithNoEndpoints = transportConfigurations.Where(tc => !tc.Endpoints.Any())
                .Select(t => t.Name);

            if (tranportsWithNoEndpoints.Any())
            {
                var endpointNames = string.Join(", ", tranportsWithNoEndpoints);
                throw new InvalidOperationException($"At least one endpoint needs to be configured for transport(s): {endpointNames}");
            }

            var allEndpoints = transportConfigurations
                .SelectMany(t => t.Endpoints);

            var duplicatedEndpoints = allEndpoints
                .GroupBy(e => e.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicatedEndpoints.Any())
            {
                var endpointNames = string.Join(", ", duplicatedEndpoints);
                throw new InvalidOperationException($"Endpoints can only be associated with a single transport, please remove endpoint(s): {endpointNames} from one transport");
            }

            var transportsWithMappedErrorQueue = transportConfigurations.Where(tc => tc.Endpoints.Any(e => e.Name.ToLower() == tc.ErrorQueue.ToLower()));

            if (transportsWithMappedErrorQueue.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine("It is not allowed to register the bridge error queue as an endpoint, please change the error queue or remove the endpoint mapping:");
                sb.AppendLine();

                foreach (var transport in transportsWithMappedErrorQueue)
                {
                    sb.AppendLine($"- Transport: {transport.Name} | ErrorQueue/EndpointName: {transport.ErrorQueue}");
                }
                throw new InvalidOperationException(sb.ToString());
            }

            var eventsWithNoRegisteredPublisher = transportConfigurations
               .SelectMany(t => t.Endpoints)
               .SelectMany(e => e.Subscriptions)
               .Where(s => !allEndpoints.Any(e => e.Name == s.Publisher));

            if (eventsWithNoRegisteredPublisher.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine("The following events have a publisher configured that is unknown:");
                sb.AppendLine();
                foreach (var eventType in eventsWithNoRegisteredPublisher)
                {
                    sb.AppendLine($"- {eventType.EventTypeFullName}, publisher: {eventType.Publisher}");
                }
                throw new InvalidOperationException(sb.ToString());
            }

            var eventsWithMultiplePublishers = transportConfigurations
                .SelectMany(t => t.Endpoints)
                .SelectMany(e => e.Subscriptions)
                .GroupBy(e => e.EventTypeFullName)
                .Where(g => g.GroupBy(s => s.Publisher).Count() > 1);

            if (eventsWithMultiplePublishers.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine("Events can only be associated with a single publisher, please verify subscriptions for:");
                sb.AppendLine();
                foreach (var eventType in eventsWithMultiplePublishers)
                {
                    var publishers = string.Join(", ", eventType.Select(e => e.Publisher));
                    sb.AppendLine($"- {eventType.Key}, registered publishers: {publishers}");
                }
                throw new InvalidOperationException(sb.ToString());
            }

            // determine transaction mode
            var transactionScopeCapableTransports = transportConfigurations
                .Where(tc => tc.TransportDefinition.GetSupportedTransactionModes()
                    .Contains(TransportTransactionMode.TransactionScope));

            TransportTransactionMode transportTransactionMode;

            if (transactionScopeCapableTransports.Count() != transportConfigurations.Count())
            {
                transportTransactionMode = TransportTransactionMode.ReceiveOnly;

                logger.LogInformation("Bridge transaction mode defaulted to TransportTransactionMode.ReceiveOnly");
            }
            else
            {
                if (runInReceiveOnlyTransactionMode)
                {
                    transportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    logger.LogInformation("Bridge transaction mode explicitly lowered to ReceiveOnly");
                }
                else
                {
                    transportTransactionMode = TransportTransactionMode.TransactionScope;
                    logger.LogInformation("Bridge transaction mode defaulted to TransportTransactionMode.TransactionScope since all transports supports it");
                }
            }

            foreach (var transportConfiguration in transportConfigurations)
            {
                transportConfiguration.TransportDefinition.TransportTransactionMode = transportTransactionMode;

                foreach (var endpoint in transportConfiguration.Endpoints)
                {
                    if (string.IsNullOrEmpty(endpoint.QueueAddress))
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        endpoint.QueueAddress = transportConfiguration.TransportDefinition
                            .ToTransportAddress(new QueueAddress(endpoint.Name));
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                }
            }

            return new FinalizedBridgeConfiguration(transportConfigurations);
        }

        bool runInReceiveOnlyTransactionMode;

        readonly List<BridgeTransport> transportConfigurations = new List<BridgeTransport>();
    }
}
