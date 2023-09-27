namespace NServiceBus
{
    using Extensibility;
    using NServiceBus.MessagingBridge.Msmq;
    using Transport;

    /// <summary>
    /// Gives users fine grained control over routing via extension methods.
    /// </summary>
    public static class JournalOptionExtensions
    {
        const string KeyJournaling = "MSMQ.UseJournalQueue";

        /// <summary>
        /// Enable or disable MSMQ journaling.
        /// </summary>
        /// <param name="options">Option being extended.</param>
        /// <param name="enable">Either enable or disabling message journaling.</param>
        public static void UseJournalQueue(this ExtendableOptions options, bool enable = true)
        {
            Guard.AgainstNull(nameof(options), options);

            options.GetDispatchProperties()[KeyJournaling] = enable.ToString();
        }

        internal static bool? ShouldUseJournalQueue(this DispatchProperties dispatchProperties)
        {
            if (dispatchProperties.TryGetValue(KeyJournaling, out var boolString))
            {
                return bool.Parse(boolString);
            }

            return null;
        }
    }
}
