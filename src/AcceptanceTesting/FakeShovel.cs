namespace AcceptanceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class FakeShovelHeader
    {
        public const string FailureHeader = "FakeShovelFailure";
    }

    class FakeShovel : IMessageShovel
    {
        readonly IMessageShovel messageShovel;
        readonly ILogger logger;

        public FakeShovel(MessageShovel shovel, ILogger logger)
        {
            messageShovel = shovel;
            this.logger = logger;
        }

        public Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default)
        {
            try
            {
                var messageContext = transferContext.MessageToTransfer;
                if (messageContext.Headers.ContainsKey(FakeShovelHeader.FailureHeader))
                {
                    throw new Exception(
                        "Incoming message has `FakeShovelFailure` header to test infrastructure failures");
                }

                return messageShovel.TransferMessage(transferContext, cancellationToken);
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError("Failed to transfer message", e);
                throw;
            }
        }
    }
}