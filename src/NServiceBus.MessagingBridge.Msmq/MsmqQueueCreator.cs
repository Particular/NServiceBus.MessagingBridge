namespace NServiceBus.MessagingBridge.Msmq
{
    using System.Collections.Generic;
    using MSMQ.Messaging;
    using System.Security.Principal;
    using Logging;

    class MsmqQueueCreator
    {
        public MsmqQueueCreator(bool useTransactionalQueues, string installerUser)
        {
            this.useTransactionalQueues = useTransactionalQueues;
            this.installerUser = installerUser;
        }

        public void CreateQueueIfNecessary(IEnumerable<string> queues)
        {
            foreach (var queue in queues)
            {
                CreateQueueIfNecessary(queue);
            }
        }

        void CreateQueueIfNecessary(string address)
        {
            var msmqAddress = MsmqAddress.Parse(address);

            Logger.Debug($"Creating '{address}' if needed.");

            if (msmqAddress.IsRemote())
            {
                Logger.Info($"'{address}' is a remote queue and won't be created");
                return;
            }

            var queuePath = msmqAddress.PathWithoutPrefix;

            if (MessageQueue.Exists(queuePath))
            {
                Logger.Debug($"'{address}' already exists");
                return;
            }

            try
            {
                using (var queue = MessageQueue.Create(queuePath, useTransactionalQueues))
                {
                    Logger.Debug($"Created queue, path: [{queuePath}], identity: [{installerUser}], transactional: [{useTransactionalQueues}]");

                    try
                    {
                        queue.SetPermissions(installerUser, MessageQueueAccessRights.WriteMessage);
                        queue.SetPermissions(installerUser, MessageQueueAccessRights.ReceiveMessage);
                        queue.SetPermissions(installerUser, MessageQueueAccessRights.PeekMessage);
                        queue.SetPermissions(installerUser, MessageQueueAccessRights.GetQueueProperties);

                        queue.SetPermissions(LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl);
                    }
                    catch (MessageQueueException permissionException) when (permissionException.MessageQueueErrorCode == MessageQueueErrorCode.FormatNameBufferTooSmall)
                    {
                        Logger.Warn($"The name for queue '{queue.FormatName}' is too long for permissions to be applied. Please consider a shorter endpoint name.", permissionException);
                    }
                }
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueExists)
            {
                //Solves the race condition problem when multiple endpoints try to create same queue (e.g. error queue).
            }
        }

        readonly bool useTransactionalQueues;
        readonly string installerUser;

        static readonly string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        static readonly ILog Logger = LogManager.GetLogger<MsmqQueueCreator>();
    }
}