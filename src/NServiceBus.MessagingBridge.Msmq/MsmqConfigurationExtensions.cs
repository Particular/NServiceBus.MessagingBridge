namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Transactions;
    using Configuration.AdvancedExtensibility;
    using NServiceBus.MessagingBridge.Msmq;
    using Routing;

    /// <summary>
    /// Adds extensions methods to <see cref="TransportExtensions{T}" /> for configuration purposes.
    /// </summary>
    public static partial class MsmqConfigurationExtensions
    {
        /// <summary>
        /// Configures the endpoint to use MSMQ to send and receive messages.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> UseTransport<TTransport>(this EndpointConfiguration endpointConfiguration)
            where TTransport : MsmqBridgeTransport
        {
            var msmqTransport = new MsmqBridgeTransport();
            var routingSettings = endpointConfiguration.UseTransport(msmqTransport);
            return new TransportExtensions<MsmqBridgeTransport>(msmqTransport, routingSettings);
        }

        /// <summary>
        /// Sets a distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="config">MSMQ Transport configuration object.</param>
        /// <param name="distributionStrategy">The instance of a distribution strategy.</param>
        public static void SetMessageDistributionStrategy(this RoutingSettings<MsmqBridgeTransport> config, DistributionStrategy distributionStrategy)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(distributionStrategy), distributionStrategy);

            config.GetSettings().GetOrCreate<List<DistributionStrategy>>().Add(distributionStrategy);
        }

        /// <summary>
        /// Returns the configuration options for the file based instance mapping file.
        /// </summary>
        /// <param name="config">MSMQ Transport configuration object.</param>
        public static InstanceMappingFileSettings InstanceMappingFile(this RoutingSettings<MsmqBridgeTransport> config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new InstanceMappingFileSettings(config.GetSettings());
        }

        /// <summary>
        /// Set a delegate to use for applying the <see cref="Label" /> property when sending a message.
        /// </summary>
        /// <remarks>
        /// This delegate will be used for all valid messages sent via MSMQ.
        /// This includes, not just standard messages, but also Audits, Errors and all control messages.
        /// In some cases it may be useful to use the <see cref="Headers.ControlMessageHeader" /> key to determine if a message is
        /// a control message.
        /// The only exception to this rule is received messages with corrupted headers. These messages will be forwarded to the
        /// error queue with no label applied.
        /// </remarks>
        public static TransportExtensions<MsmqBridgeTransport> ApplyLabelToMessages(
            this TransportExtensions<MsmqBridgeTransport> transport,
            Func<IReadOnlyDictionary<string, string>, string> labelGenerator)
        {
            transport.Transport.ApplyCustomLabelToOutgoingMessages = labelGenerator;
            return transport;
        }

        /// <summary>
        /// Allows to change the transaction isolation level and timeout for the `TransactionScope` used to receive messages.
        /// </summary>
        /// <remarks>
        /// If not specified the default transaction timeout of the machine will be used and the isolation level will be set to
        /// <see cref="IsolationLevel.ReadCommitted"/>.
        /// </remarks>
        /// <param name="transport">The transport settings to configure.</param>
        /// <param name="timeout">Transaction timeout duration.</param>
        /// <param name="isolationLevel">Transaction isolation level.</param>
        public static TransportExtensions<MsmqBridgeTransport> TransactionScopeOptions(
            this TransportExtensions<MsmqBridgeTransport> transport,
            TimeSpan? timeout = null,
            IsolationLevel? isolationLevel = null)
        {
            transport.Transport.ConfigureTransactionScope(timeout, isolationLevel);
            return transport;
        }

        /// <summary>
        /// Moves messages that have exceeded their TimeToBeReceived to the dead letter queue instead of discarding them.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> UseDeadLetterQueueForMessagesWithTimeToBeReceived(
            this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.UseDeadLetterQueueForMessagesWithTimeToBeReceived = true;
            return transport;
        }

        /// <summary>
        /// Disables the automatic queue creation when installers are enabled using `EndpointConfiguration.EnableInstallers()`.
        /// </summary>
        /// <remarks>
        /// With installers enabled, required queues will be created automatically at startup.While this may be convenient for development,
        /// we instead recommend that queues are created as part of deployment using the CreateQueues.ps1 script included in the NuGet package.
        /// The installers might still need to be enabled to fulfill the installation needs of other components, but this method allows
        /// scripts to be used for queue creation instead.
        /// </remarks>
        public static TransportExtensions<MsmqBridgeTransport> DisableInstaller(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.CreateQueues = false;
            return transport;
        }

        /// <summary>
        /// This setting should be used with caution. It disables the storing of undeliverable messages
        /// in the dead letter queue. Therefore this setting must only be used where loss of messages
        /// is an acceptable scenario.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> DisableDeadLetterQueueing(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.UseDeadLetterQueue = false;
            return transport;
        }

        /// <summary>
        /// Instructs MSMQ to cache connections to a remote queue and re-use them
        /// as needed instead of creating new connections for each message.
        /// Turning connection caching off will negatively impact the message throughput in
        /// most scenarios.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> DisableConnectionCachingForSends(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.UseConnectionCache = false;
            return transport;
        }

        /// <summary>
        /// This setting should be used with caution. As the queues are not transactional, any message that has
        /// an exception during processing will not be rolled back to the queue. Therefore this setting must only
        /// be used where loss of messages is an acceptable scenario.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> UseNonTransactionalQueues(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.UseTransactionalQueues = false;
            return transport;
        }

        /// <summary>
        /// Enables the use of journaling messages. Stores a copy of every message received in the journal queue.
        /// Should be used ONLY when debugging as it can
        /// potentially use up the MSMQ journal storage quota based on the message volume.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> EnableJournaling(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.UseJournalQueue = true;
            return transport;
        }

        /// <summary>
        /// Overrides the Time-To-Reach-Queue (TTRQ) timespan. The default value if not set is Message.InfiniteTimeout
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> TimeToReachQueue(this TransportExtensions<MsmqBridgeTransport> transport, TimeSpan timeToReachQueue)
        {
            transport.Transport.TimeToReachQueue = timeToReachQueue;
            return transport;
        }

        /// <summary>
        /// Disables native Time-To-Be-Received (TTBR) when combined with transactions.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> DisableNativeTimeToBeReceivedInTransactions(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.UseNonNativeTimeToBeReceivedInTransactions = true;
            return transport;
        }

        /// <summary>
        /// Configures native delayed delivery.
        /// </summary>
        public static DelayedDeliverySettings NativeDelayedDelivery(this TransportExtensions<MsmqBridgeTransport> config, IDelayedMessageStore delayedMessageStore)
        {
            Guard.AgainstNull(nameof(delayedMessageStore), delayedMessageStore);
            config.Transport.DelayedDelivery = new DelayedDeliverySettings(delayedMessageStore);
            return config.Transport.DelayedDelivery;
        }

        /// <summary>
        /// Ignore incoming Time-To-Be-Received (TTBR) headers. By default an expired TTBR header will result in the message to be discarded.
        /// </summary>
        public static TransportExtensions<MsmqBridgeTransport> IgnoreIncomingTimeToBeReceivedHeaders(this TransportExtensions<MsmqBridgeTransport> transport)
        {
            transport.Transport.IgnoreIncomingTimeToBeReceivedHeaders = true;
            return transport;
        }
    }
}