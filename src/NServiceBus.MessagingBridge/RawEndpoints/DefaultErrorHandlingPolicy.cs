namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class DefaultErrorHandlingPolicy : IErrorHandlingPolicy
    {
        public DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            this.errorQueue = errorQueue;
            this.immediateRetryCount = immediateRetryCount;
        }

        public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher, CancellationToken cancellationToken = default)
        {
            if (handlingContext.Error.ImmediateProcessingFailures < immediateRetryCount)
            {
                return RetryRequiredTask;
            }
            return handlingContext.MoveToErrorQueue(errorQueue, cancellationToken: cancellationToken);
        }

        readonly string errorQueue;
        readonly int immediateRetryCount;

        static Task<ErrorHandleResult> RetryRequiredTask = Task.FromResult(ErrorHandleResult.RetryRequired);
    }
}