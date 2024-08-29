namespace AcceptanceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class FakeShovelHeader
    {
        public const string FailureHeader = "FakeShovelFailure";
    }

    class FakeShovel(MessageShovel shovel) : IMessageShovel
    {
        readonly IMessageShovel messageShovel = shovel;

        public Task TransferMessage(TransferContext transferContext,
            CancellationToken cancellationToken = default)
        {
            var messageContext = transferContext.MessageToTransfer;
            if (messageContext.Headers.ContainsKey(FakeShovelHeader.FailureHeader))
            {
                throw new Exception("Incoming message has `FakeShovelFailure` header to test infrastructure failures");
            }

            return messageShovel.TransferMessage(transferContext, cancellationToken);
        }
    }
}