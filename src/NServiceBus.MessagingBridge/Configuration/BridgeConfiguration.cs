namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Configuration options for bridging multiple transports
    /// </summary>
    public partial class BridgeConfiguration
    {
        /// <summary>
        /// Configures the bridge with the given transport
        /// </summary>
        public void AddTransport(BridgeTransport transportConfiguration)
        {
            ArgumentNullException.ThrowIfNull(transportConfiguration);

            if (transportConfigurations.Any(t => t.Name == transportConfiguration.Name))
            {
                throw new InvalidOperationException($"A transport with the name {transportConfiguration.Name} has already been configured. Use a different transport type or specify a custom name");
            }

            transportConfigurations.Add(transportConfiguration);
        }

        /// <summary>
        /// Runs the bridge in receive-only transaction mode regardless of whether the bridged transports support distributed transactions
        /// </summary>
        public void RunInReceiveOnlyTransactionMode() => runInReceiveOnlyTransactionMode = true;

        /// <summary>
        /// Disables the enforcement of messaging best practices (e.g. validating that an event has only one logical publisher).
        /// </summary>
        public void DoNotEnforceBestPractices() => allowMultiplePublishersSameEvent = true;

        /// <summary>
        /// Disable ReplyTo address translation on the bridge for failed messages
        /// </summary>
        public void DoNotTranslateReplyToAddressForFailedMessages() => translateReplyToAddressForFailedMessages = false;

        internal FinalizedBridgeConfiguration FinalizeConfiguration(ILogger<BridgeConfiguration> logger)
        {
            if (transportConfigurations.Count < 2)
            {
                throw new InvalidOperationException("At least two transports needs to be configured");
            }

            var allEndpoints = transportConfigurations
                .SelectMany(t => t.Endpoints).ToArray();

            if (!allEndpoints.Any())
            {
                throw new InvalidOperationException($"At least one endpoint needs to be configured");
            }

            var duplicatedEndpoints = allEndpoints
                .GroupBy(e => e.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();

            if (duplicatedEndpoints.Any())
            {
                var endpointNames = string.Join(", ", duplicatedEndpoints);
                throw new InvalidOperationException($"Endpoints can only be associated with a single transport, please remove endpoint(s): {endpointNames} from one transport");
            }

            var transportsWithMappedErrorQueue = transportConfigurations
                .Where(tc => tc.Endpoints.Any(e => string.Equals(e.Name, tc.ErrorQueue, StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();

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
               .Where(s => allEndpoints.All(e => e.Name != s.Publisher))
               .ToArray();

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
                .Where(g => g.GroupBy(s => s.Publisher).Count() > 1)
                .ToArray();

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
            }

            return new FinalizedBridgeConfiguration(transportConfigurations, translateReplyToAddressForFailedMessages);
        }

        bool runInReceiveOnlyTransactionMode;
        bool allowMultiplePublishersSameEvent;
        bool translateReplyToAddressForFailedMessages = true;

        readonly List<BridgeTransport> transportConfigurations = [];
    }
}