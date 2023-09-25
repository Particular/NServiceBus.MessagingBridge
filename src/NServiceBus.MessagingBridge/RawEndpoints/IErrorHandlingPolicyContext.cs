namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Context for error handling policy.
    /// </summary>
    interface IErrorHandlingPolicyContext
    {
        /// <summary>
        /// Moves a given message to the error queue.
        /// </summary>
        Task<ErrorHandleResult> MoveToErrorQueue(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the error information.
        /// </summary>
        ErrorContext Error { get; }

        /// <summary>
        /// The queue from which the failed message has been received.
        /// </summary>
        string FailedQueue { get; }

        /// <summary>
        /// The queue to which the failed will be moved if `MoveToErrorQueue` is called
        /// </summary>
        string ErrorQueue { get; }
    }
}