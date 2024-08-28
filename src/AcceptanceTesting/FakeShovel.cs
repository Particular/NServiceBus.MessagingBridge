namespace AcceptanceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;

    public class FakeShovelHeader
    {
        public const string FailureHeader = "FakeShovelFailure";
    }

    class FakeShovel(MessageShovel shovel) : IMessageShovel
    {
        readonly IMessageShovel messageShovel = shovel;

        public async Task TransferMessage(TransferContext transferContext,
            CancellationToken cancellationToken = default)
        {
            var messageContext = transferContext.MessageToTransfer;
            if (messageContext.Headers.ContainsKey(FakeShovelHeader.FailureHeader))
            {
                throw new Exception("Incoming message has `FakeShovelFailure` header to test infrastructure failures");
            }

            try
            {
                await messageShovel.TransferMessage(transferContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                throw;
            }
            catch (Exception e)
            {
                Assert.Fail("Message shoveling failed: " + e.Message);
            }
        }
    }
}