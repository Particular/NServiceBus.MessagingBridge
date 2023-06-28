namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class DefaultErrorHandlingPolicy : IErrorHandlingPolicy
    {
        string errorQueue;
        int immediateRetryCount;

        public DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            this.errorQueue = errorQueue;
            this.immediateRetryCount = immediateRetryCount;
        }

        public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher, CancellationToken cancellationToken = default)
        {
            if (handlingContext.Error.ImmediateProcessingFailures < immediateRetryCount)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
            return handlingContext.MoveToErrorQueue(errorQueue, cancellationToken: cancellationToken);
        }
    }
}