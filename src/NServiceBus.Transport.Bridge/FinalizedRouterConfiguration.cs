using System.Collections.Generic;

public class FinalizedRouterConfiguration
{
    public FinalizedRouterConfiguration(List<TransportConfiguration> transports) => Transports = transports;

    public List<TransportConfiguration> Transports { get; }
}