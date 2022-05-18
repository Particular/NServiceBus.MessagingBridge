using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus.Raw;
using NServiceBus.Transport;

class MessageShovelErrorHandlingPolicy : IErrorHandlingPolicy
{
    public MessageShovelErrorHandlingPolicy(ILogger<MessageShovelErrorHandlingPolicy> logger, string errorQueue)
    {
        this.logger = logger;
        this.errorQueue = errorQueue;
    }

    public Task<ErrorHandleResult> OnError(
        IErrorHandlingPolicyContext handlingContext,
        IMessageDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (handlingContext.Error.ImmediateProcessingFailures < 3)
        {
            logger.LogWarning("Message shovel operation failed and will be retried", handlingContext.Error.Exception);
            return Task.FromResult(ErrorHandleResult.RetryRequired);
        }

        logger.LogError($"Message shovel operation failed, message will be moved to {errorQueue}", handlingContext.Error.Exception);

        return handlingContext.MoveToErrorQueue(errorQueue, cancellationToken: cancellationToken);
    }

    readonly ILogger<MessageShovelErrorHandlingPolicy> logger;
    readonly string errorQueue;
}