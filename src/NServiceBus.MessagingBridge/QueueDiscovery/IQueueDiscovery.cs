namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Provides queue names dynamically for bridge endpoints
/// </summary>
public interface IQueueDiscovery
{
    /// <summary>
    /// Get queue names to monitor
    /// </summary>
    IAsyncEnumerable<string> GetQueues(CancellationToken cancellationToken = default);
}
