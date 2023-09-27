namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using NServiceBus.MessagingBridge.Msmq;
    using Settings;

    /// <summary>
    /// Provides configuration extensions when using <see cref="MsmqPersistence"/>.
    /// </summary>
    public static class MsmqSubscriptionStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures the queue used to store subscriptions.
        /// </summary>
        /// <param name="persistenceExtensions">The settings to extend.</param>
        /// <param name="queue">The queue name.</param>
        public static void SubscriptionQueue(this PersistenceExtensions<MsmqPersistence> persistenceExtensions, string queue)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(queue), queue);

            persistenceExtensions.GetSettings().Set(MsmqPersistenceQueueConfigurationKey, queue);
        }

        internal static string GetConfiguredMsmqPersistenceSubscriptionQueue(this IReadOnlySettings settings)
        {
            return settings.GetOrDefault<string>(MsmqPersistenceQueueConfigurationKey);
        }

        internal const string MsmqPersistenceQueueConfigurationKey = "MsmqSubscriptionPersistence.QueueName";
    }
}