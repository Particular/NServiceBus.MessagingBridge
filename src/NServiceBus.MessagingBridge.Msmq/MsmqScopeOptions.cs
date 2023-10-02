namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Transactions;

    class MsmqScopeOptions
    {
        public MsmqScopeOptions(TimeSpan? requestedTimeout = null, IsolationLevel? requestedIsolationLevel = null)
        {
            var timeout = TransactionManager.DefaultTimeout;
            var isolationLevel = IsolationLevel.ReadCommitted;
            if (requestedTimeout.HasValue)
            {
                if (requestedTimeout.Value > TransactionManager.MaximumTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(requestedTimeout), requestedTimeout.Value, "Timeout requested is longer than the maximum value for this machine. Override using the maxTimeout setting of the system.transactions section in machine.config");
                }

                timeout = requestedTimeout.Value;
            }

            if (requestedIsolationLevel.HasValue)
            {
                isolationLevel = requestedIsolationLevel.Value;
            }

            TransactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout
            };
        }

        public TransactionOptions TransactionOptions { get; }
    }
}