namespace NServiceBus.Raw
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Routing;
    using NServiceBus.Support;
    using NServiceBus.Transport;

    class RawEndpointErrorHandlingPolicy
    {
        string localAddress;
        IMessageDispatcher dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        IErrorHandlingPolicy policy;

        public RawEndpointErrorHandlingPolicy(string endpointName, string localAddress, IMessageDispatcher dispatcher, IErrorHandlingPolicy policy)
        {
            this.localAddress = localAddress;
            this.dispatcher = dispatcher;
            this.policy = policy;

            staticFaultMetadata = new Dictionary<string, string>
            {
                {FaultsHeaderKeys.FailedQ, localAddress},
                {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                {Headers.ProcessingEndpoint, endpointName},
            };
        }

        public Task<ErrorHandleResult> OnError(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            return policy.OnError(new Context(localAddress, errorContext, MoveToErrorQueue), dispatcher, cancellationToken);
        }

        async Task<ErrorHandleResult> MoveToErrorQueue(ErrorContext errorContext, string errorQueue, bool includeStandardHeaders, CancellationToken cancellationToken)
        {
            var message = errorContext.Message;

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            ExceptionHeaderHelper.SetExceptionHeaders(headers, errorContext.Exception);

            if (includeStandardHeaders)
            {
                foreach (var faultMetadata in staticFaultMetadata)
                {
                    headers[faultMetadata.Key] = faultMetadata.Value;
                }
            }
            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueue)));

            await dispatcher.Dispatch(transportOperations, errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }

        class Context : IErrorHandlingPolicyContext
        {
            Func<ErrorContext, string, bool, CancellationToken, Task<ErrorHandleResult>> moveToErrorQueue;

            public Context(string failedQueue, ErrorContext error, Func<ErrorContext, string, bool, CancellationToken, Task<ErrorHandleResult>> moveToErrorQueue)
            {
                this.moveToErrorQueue = moveToErrorQueue;
                Error = error;
                FailedQueue = failedQueue;
            }

            public Task<ErrorHandleResult> MoveToErrorQueue(string errorQueue, bool attachStandardFailureHeaders = true, CancellationToken cancellationToken = default)
            {
                return moveToErrorQueue(Error, errorQueue, attachStandardFailureHeaders, cancellationToken);
            }

            public ErrorContext Error { get; }
            public string FailedQueue { get; }
        }
    }
}