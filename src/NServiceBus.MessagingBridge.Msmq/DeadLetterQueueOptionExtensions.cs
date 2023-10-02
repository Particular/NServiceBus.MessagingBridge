namespace NServiceBus
{
    using Extensibility;
    using NServiceBus.MessagingBridge.Msmq;
    using Transport;

    /// <summary>
    /// Gives users fine grained control over routing via extension methods.
    /// </summary>
    public static class DeadLetterQueueOptionExtensions
    {
        const string KeyDeadLetterQueue = "MSMQ.UseDeadLetterQueue";

        /// <summary>
        /// Enable or disable MSMQ dead letter queueing.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <param name="enable">Either enable or disabling message dead letter queueing.</param>
        public static void UseDeadLetterQueue(this ExtendableOptions options, bool enable = true)
        {
            Guard.AgainstNull(nameof(options), options);
            options.GetDispatchProperties()[KeyDeadLetterQueue] = enable.ToString();
        }

        internal static bool? ShouldUseDeadLetterQueue(this DispatchProperties dispatchProperties)
        {
            if (dispatchProperties.TryGetValue(KeyDeadLetterQueue, out var boolString))
            {
                return bool.Parse(boolString);
            }

            return null;
        }
    }
}
