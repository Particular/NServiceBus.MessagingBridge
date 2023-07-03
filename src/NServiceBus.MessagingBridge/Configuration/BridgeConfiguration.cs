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
            Guard.AgainstNull(nameof(transportConfiguration), transportConfiguration);

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

        /// <summary>
        /// Disables the enforcement of messaging best practices (e.g. validating that an event has only one logical publisher).
        /// </summary>
        public void DoNotEnforceBestPractices() => allowMultiplePublishersSameEvent = true;

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
                logger.LogWarning("The following transport(s) have no endpoints: {endpointNames}", endpointNames);
            }

            if (tranportsWithNoEndpoints.Count() == transportConfigurations.Count)
            {
                throw new InvalidOperationException("No transport has an endpoint configured. At least one transport should have an endpoint to be able to bridge messages");
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
                    sb.AppendLine($"- {eventType.EventTypeAssemblyQualifiedName}, publisher: {eventType.Publisher}");
                }
                throw new InvalidOperationException(sb.ToString());
            }

            var eventsWithMultiplePublishers = transportConfigurations
                .SelectMany(t => t.Endpoints)
                .SelectMany(e => e.Subscriptions)
                .GroupBy(e => e.EventTypeAssemblyQualifiedName)
                .Where(g => g.GroupBy(s => s.Publisher).Count() > 1);

            if (eventsWithMultiplePublishers.Any())
            {
                var data = new StringBuilder();

                foreach (var eventType in eventsWithMultiplePublishers)
                {
                    var publishers = string.Join(", ", eventType.Select(e => e.Publisher));
                    data.Append($"- {eventType.Key}, registered publishers: {publishers}\r");
                }

                if (allowMultiplePublishersSameEvent)
                {
                    logger.LogWarning("The following subscriptions with multiple registered publishers are ignored as best practices are not enforced:\r{events}", data);
                }
                else
                {
                    throw new InvalidOperationException("Events can only be associated with a single publisher, please verify subscriptions for:\r" + data);
                }
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
        bool allowMultiplePublishersSameEvent;

        readonly List<BridgeTransport> transportConfigurations = new List<BridgeTransport>();
    }
}