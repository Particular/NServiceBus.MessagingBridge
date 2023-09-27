
namespace NServiceBus.MessagingBridge.Msmq
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Support;
    using Transport;

    class MsmqTransportInfrastructure : TransportInfrastructure
    {
        readonly DelayedDeliveryPump delayedDeliveryPump;

        public MsmqTransportInfrastructure(IReadOnlyDictionary<string, IMessageReceiver> receivers, MsmqMessageDispatcher dispatcher, DelayedDeliveryPump delayedDeliveryPump)
        {
            this.delayedDeliveryPump = delayedDeliveryPump;
            Dispatcher = dispatcher;
            Receivers = receivers;
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            if (delayedDeliveryPump != null)
            {
                await delayedDeliveryPump.Start(cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task Shutdown(CancellationToken cancellationToken = default)
        {
            if (delayedDeliveryPump != null)
            {
                await delayedDeliveryPump.Stop(cancellationToken).ConfigureAwait(false);
            }
        }

        public override string ToTransportAddress(QueueAddress address) => TranslateAddress(address);

        internal static string TranslateAddress(QueueAddress address)
        {
            if (!address.Properties.TryGetValue("machine", out var machine))
            {
                machine = RuntimeEnvironment.MachineName;
            }
            if (!address.Properties.TryGetValue("queue", out var queueName))
            {
                queueName = address.BaseAddress;
            }

            var queue = new StringBuilder(queueName);
            if (address.Discriminator != null)
            {
                queue.Append("-" + address.Discriminator);
            }
            if (address.Qualifier != null)
            {
                queue.Append("." + address.Qualifier);
            }
            return $"{queue}@{machine}";
        }
    }
}