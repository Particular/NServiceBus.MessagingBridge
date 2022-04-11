namespace NServiceBus.Raw
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Transport;

    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class RawEndpointConfiguration
    {
        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        public static RawEndpointConfiguration CreateSendOnly(string endpointName, TransportDefinition transportDefinition)
        {
            return new RawEndpointConfiguration(endpointName, transportDefinition, null, null);
        }

        /// <summary>
        /// Creates a regular raw endpoint config.
        /// </summary>
        public static RawEndpointConfiguration Create(
            string endpointName,
            TransportDefinition transportDefinition,
            Func<MessageContext, IMessageDispatcher, CancellationToken, Task> onMessage, //TODO: add cancellation token
            string poisonMessageQueue)
        {
            return new RawEndpointConfiguration(endpointName, transportDefinition, onMessage, poisonMessageQueue);
        }

        RawEndpointConfiguration(
            string endpointName,
            TransportDefinition transportDefinition,
            Func<MessageContext, IMessageDispatcher, CancellationToken, Task> onMessage,
            string poisonMessageQueue)
        {
            ValidateEndpointName(endpointName);

            EndpointName = endpointName;
            TransportDefinition = transportDefinition;
            OnMessage = onMessage;
            PoisonMessageQueue = poisonMessageQueue;
            ErrorHandlingPolicy = new DefaultErrorHandlingPolicy(poisonMessageQueue, 3);
            SendOnly = onMessage == null;
            PushRuntimeSettings = PushRuntimeSettings.Default;
            OnCriticalError = (_, __) => Task.CompletedTask;
            AdditionalQueues = new string[0];
        }

        /// <summary>
        /// Instructs the endpoint to use a custom error handling policy.
        /// </summary>
        public void CustomErrorHandlingPolicy(IErrorHandlingPolicy customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            ErrorHandlingPolicy = customPolicy;
        }

        /// <summary>
        /// Instructs the endpoint to automatically create input queue, poison queue and optionally additional queues if they do not exist.
        /// </summary>
        public void AutoCreateQueues(string[] additionalQueues = null)
        {
            SetupInfrastructure = true;

            if (additionalQueues != null)
            {
                AdditionalQueues = additionalQueues;
            }
        }

        /// <summary>
        /// Customizes the behavior should a critical error occur
        /// </summary>
        public void CriticalErrorAction(Func<ICriticalErrorContext, CancellationToken, Task> criticalErrorAction)
        {
            Guard.AgainstNull(nameof(criticalErrorAction), criticalErrorAction);

            OnCriticalError = criticalErrorAction;
        }

        /// <summary>
        /// Instructs the endpoint to not enable the pub/sub capabilities of the transport.
        /// </summary>
        public void DisablePublishAndSubscribe()
        {
            PublishAndSubscribeDisabled = true;
        }

        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        /// <param name="maxConcurrency">The max concurrency allowed.</param>
        public void LimitMessageProcessingConcurrencyTo(int maxConcurrency)
        {
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), maxConcurrency);

            PushRuntimeSettings = new PushRuntimeSettings(maxConcurrency);
        }

        internal InitializableRawEndpoint Build()
        {
            return new InitializableRawEndpoint(this);
        }

        static void ValidateEndpointName(string endpointName)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            if (endpointName.Contains("@"))
            {
                throw new ArgumentException("Endpoint name must not contain an '@' character.", nameof(endpointName));
            }
        }

        internal bool SendOnly { get; private set; }
        internal IErrorHandlingPolicy ErrorHandlingPolicy { get; private set; }
        internal Func<MessageContext, IMessageDispatcher, CancellationToken, Task> OnMessage { get; private set; }
        internal string PoisonMessageQueue { get; private set; }
        internal string EndpointName { get; private set; }
        internal TransportDefinition TransportDefinition { get; private set; }
        internal PushRuntimeSettings PushRuntimeSettings { get; private set; }
        internal bool SetupInfrastructure { get; private set; }
        internal string[] AdditionalQueues { get; private set; }
        internal bool PublishAndSubscribeDisabled { get; private set; }
        internal Func<ICriticalErrorContext, CancellationToken, Task> OnCriticalError { get; private set; }
    }
}