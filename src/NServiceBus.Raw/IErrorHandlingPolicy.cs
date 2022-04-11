namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Represents a policy for handling errors.
    /// </summary>
    public interface IErrorHandlingPolicy
    {
        /// <summary>
        /// Invoked when an error occurs while processing a message.
        /// </summary>
        Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher, CancellationToken cancellationToken = default);
    }
}