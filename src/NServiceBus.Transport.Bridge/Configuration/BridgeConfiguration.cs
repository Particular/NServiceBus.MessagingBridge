namespace NServiceBus
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.Logging;

    public class BridgeConfiguration
    {
        public void AddTransport(BridgeTransportConfiguration transportConfiguration)
        {
            if (transportConfigurations.Any(t => t.Name == transportConfiguration.Name))
            {
                throw new InvalidOperationException($"A transport with the name {transportConfiguration.Name} has already been configured. Use a different transport type or specify a custom name");
            }

            transportConfigurations.Add(transportConfiguration);
        }

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

            var eventsWithNoRegisteredPublisher = transportConfigurations
               .SelectMany(t => t.Endpoints)
               .SelectMany(e => e.Subscriptions)
               .Where(s => !allEndpoints.Any(e => e.Name == s.Publisher));

            if (eventsWithNoRegisteredPublisher.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine("Publisher not registered for events:");
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
            }

            return new FinalizedBridgeConfiguration(transportConfigurations);
        }

        bool runInReceiveOnlyTransactionMode;

        readonly List<BridgeTransportConfiguration> transportConfigurations = new List<BridgeTransportConfiguration>();
    }
}