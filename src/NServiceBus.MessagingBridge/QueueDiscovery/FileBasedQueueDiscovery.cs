namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

/// <summary>
/// Queue discovery provider that reads queue names from a file
/// </summary>
public sealed class FileBasedQueueDiscovery : IQueueDiscovery
{
    readonly string filePath;

    /// <summary>
    /// Creates a new instance of FileBasedQueueDiscovery
    /// </summary>
    /// <param name="filePath">Path to the file containing queue names (one per line)</param>
    public FileBasedQueueDiscovery(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = filePath;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> GetQueues(CancellationToken cancellationToken = default)
    {
        // Read every time allows file to be updated without requiring to restart the host
        return File.ReadLinesAsync(filePath, cancellationToken);
    }
}
