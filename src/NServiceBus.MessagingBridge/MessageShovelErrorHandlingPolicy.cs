using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus.Raw;
using NServiceBus.Transport;

class MessageShovelErrorHandlingPolicy : IErrorHandlingPolicy
{
    public MessageShovelErrorHandlingPolicy(ILogger<MessageShovelErrorHandlingPolicy> logger)
    {
        this.logger = logger;
    }

    public Task<ErrorHandleResult> OnError(
        IErrorHandlingPolicyContext handlingContext,
        IMessageDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (handlingContext.Error.ImmediateProcessingFailures < 3)
        {
            logger.LogWarning(handlingContext.Error.Exception, "Message shovel operation failed and will be retried");
            return Task.FromResult(ErrorHandleResult.RetryRequired);
        }

        logger.LogError(handlingContext.Error.Exception, "Message shovel operation failed, message will be moved to {ErrorQueue}", handlingContext.ErrorQueue);

        return handlingContext.MoveToErrorQueue(cancellationToken);
    }

    readonly ILogger<MessageShovelErrorHandlingPolicy> logger;
}