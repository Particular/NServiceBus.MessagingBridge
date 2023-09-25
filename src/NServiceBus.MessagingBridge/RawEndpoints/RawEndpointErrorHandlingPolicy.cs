namespace NServiceBus.Raw
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    class RawEndpointErrorHandlingPolicy
    {
        public RawEndpointErrorHandlingPolicy(string localAddress, string errorQueue, IMessageDispatcher dispatcher, IErrorHandlingPolicy policy)
        {
            this.localAddress = localAddress;
            this.errorQueue = errorQueue;
            this.dispatcher = dispatcher;
            this.policy = policy;
        }

        public Task<ErrorHandleResult> OnError(ErrorContext errorContext, CancellationToken cancellationToken = default) => policy.OnError(new Context(errorQueue, localAddress, errorContext, MoveToErrorQueue), dispatcher, cancellationToken);

        async Task<ErrorHandleResult> MoveToErrorQueue(ErrorContext errorContext, string errorQueue, CancellationToken cancellationToken)
        {
            var message = errorContext.Message;

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            headers[BridgeHeaders.FailedQ] = localAddress;
            ExceptionHeaderHelper.SetExceptionHeaders(headers, errorContext.Exception);

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueue)));

            await dispatcher.Dispatch(transportOperations, errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }

        class Context : IErrorHandlingPolicyContext
        {
            Func<ErrorContext, string, CancellationToken, Task<ErrorHandleResult>> moveToErrorQueue;

            public Context(string errorQueue, string failedQueue, ErrorContext error, Func<ErrorContext, string, CancellationToken, Task<ErrorHandleResult>> moveToErrorQueue)
            {
                this.moveToErrorQueue = moveToErrorQueue;
                Error = error;
                ErrorQueue = errorQueue;
                FailedQueue = failedQueue;
            }

            public Task<ErrorHandleResult> MoveToErrorQueue(CancellationToken cancellationToken = default) => moveToErrorQueue(Error, ErrorQueue, cancellationToken);

            public ErrorContext Error { get; }
            public string FailedQueue { get; }
            public string ErrorQueue { get; }
        }

        readonly string localAddress;
        readonly string errorQueue;
        readonly IMessageDispatcher dispatcher;
        readonly IErrorHandlingPolicy policy;
    }
}