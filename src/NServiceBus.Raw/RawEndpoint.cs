namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides factory methods for creating and starting endpoint instances.
    /// </summary>
    public static class RawEndpoint
    {
        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        public static Task<IStartableRawEndpoint> Create(RawEndpointConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var initializable = configuration.Build();
            return initializable.Initialize(cancellationToken);
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        public static async Task<IReceivingRawEndpoint> Start(RawEndpointConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var initializable = await Create(configuration, cancellationToken).ConfigureAwait(false);
            return await initializable.Start(cancellationToken).ConfigureAwait(false);
        }
    }
}