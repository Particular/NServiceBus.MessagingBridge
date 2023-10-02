namespace NServiceBus.MessagingBridge.Msmq
{
    using System;

    class TimeToBeReceivedOverrideChecker
    {
        public static void Check(bool isTransactional, bool outBoxRunning, bool auditTTBROverridden)
        {
            if (!isTransactional)
            {
                return;
            }

            if (outBoxRunning)
            {
                return;
            }

            if (auditTTBROverridden)
            {
                throw new Exception("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ.");
            }
        }
    }
}