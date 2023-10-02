namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class DefaultErrorHandlingPolicy : IErrorHandlingPolicy
    {
        public DefaultErrorHandlingPolicy(int immediateRetryCount)
        {
            this.immediateRetryCount = immediateRetryCount;
        }

        public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher, CancellationToken cancellationToken = default)
        {
            if (handlingContext.Error.ImmediateProcessingFailures < immediateRetryCount)
            {
                return RetryRequiredTask;
            }
            return handlingContext.MoveToErrorQueue(cancellationToken);
        }

        readonly int immediateRetryCount;

        static Task<ErrorHandleResult> RetryRequiredTask = Task.FromResult(ErrorHandleResult.RetryRequired);
    }
}