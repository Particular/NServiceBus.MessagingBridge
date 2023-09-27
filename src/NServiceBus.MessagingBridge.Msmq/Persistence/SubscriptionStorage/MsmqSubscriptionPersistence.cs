namespace NServiceBus.Persistence.Msmq
{
    using System;
    using Features;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using MSMQ.Messaging;
    using NServiceBus.MessagingBridge.Msmq;
    using Settings;
    using Transport;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class MsmqSubscriptionPersistence : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var configuredQueueName = DetermineStorageQueueName(context.Settings);

            context.Settings.Get<QueueBindings>().BindSending(configuredQueueName);

            var transportSettings = context.Settings.Get<TransportDefinition>() as MsmqBridgeTransport;

            var queue = new MsmqSubscriptionStorageQueue(MsmqAddress.Parse(configuredQueueName), transportSettings.UseTransactionalQueues);
            var storage = new MsmqSubscriptionStorage(queue);

            context.Services.AddSingleton<ISubscriptionStorage>(storage);
        }

        internal static string DetermineStorageQueueName(IReadOnlySettings settings)
        {
            var configuredQueueName = settings.GetConfiguredMsmqPersistenceSubscriptionQueue();

            if (!string.IsNullOrEmpty(configuredQueueName))
            {
                return configuredQueueName;
            }
            ThrowIfUsingTheOldDefaultSubscriptionsQueue();

            var defaultQueueName = $"{settings.EndpointName()}.Subscriptions";
            Logger.Info($"The queue used to store subscriptions has not been configured, the default '{defaultQueueName}' will be used.");
            return defaultQueueName;
        }

        static void ThrowIfUsingTheOldDefaultSubscriptionsQueue()
        {
            if (DoesOldDefaultQueueExists())
            {
                // The user has not configured the subscriptions queue to be "NServiceBus.Subscriptions" but there's a local queue.
                // Indicates that the endpoint was using old default queue name.
                throw new Exception(
                    "Detected the presence of an old default queue named `NServiceBus.Subscriptions`. Either migrate the subscriptions to the new default queue `[Your endpoint name].Subscriptions`, see our documentation for more details, or explicitly configure the subscriptions queue name to `NServiceBus.Subscriptions` if you want to use the existing queue.");
            }
        }

        static bool DoesOldDefaultQueueExists()
        {
            const string oldDefaultSubscriptionsQueue = "NServiceBus.Subscriptions";
            var path = MsmqAddress.Parse(oldDefaultSubscriptionsQueue).PathWithoutPrefix;
            return MessageQueue.Exists(path);
        }

        static ILog Logger = LogManager.GetLogger(typeof(MsmqSubscriptionPersistence));
    }
}