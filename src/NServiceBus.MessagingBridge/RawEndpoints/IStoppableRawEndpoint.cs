namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the shutdown phase.
    /// </summary>
    interface IStoppableRawEndpoint
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop(CancellationToken cancellationToken = default);
    }
}