namespace NServiceBus
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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

        internal void Validate()
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

            var duplicatedEndpoints = transportConfigurations
                .SelectMany(t => t.Endpoints)
                .GroupBy(e => e.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicatedEndpoints.Any())
            {
                var endpointNames = string.Join(", ", duplicatedEndpoints);
                throw new InvalidOperationException($"Endpoints can only be associated with a single transport, please remove endpoint(s): {endpointNames} from one transport");
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
        }

        internal IReadOnlyCollection<BridgeTransportConfiguration> TransportConfigurations => transportConfigurations;

        readonly List<BridgeTransportConfiguration> transportConfigurations = new List<BridgeTransportConfiguration>();
    }
}