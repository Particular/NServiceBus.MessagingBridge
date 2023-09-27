namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    class AsyncTimer : IAsyncTimer
    {
        public void Start(Func<CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback)
        {
            tokenSource = new CancellationTokenSource();

            // no Task.Run() here because RunAndSwallowExceptions immediately yields with an await
            task = RunAndSwallowExceptions(callback, interval, errorCallback, tokenSource.Token);
        }

        static async Task RunAndSwallowExceptions(Func<CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await callback(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
                {
                    // private token, sender is being stopped, log the exception in case the stack trace is ever needed for debugging
                    Logger.Debug("Operation canceled while stopping timer.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    try
                    {
                        errorCallback(ex);
                    }
                    catch (Exception errorCallbackEx)
                    {
                        Logger.Error("Error callback failed.", errorCallbackEx);
                    }
                }
            }
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            tokenSource?.Cancel();

            // await the task before disposing to avoid an ObjectDisposedException when passing the token to Task.Delay or elsewhere
            await (task ?? Task.CompletedTask).ConfigureAwait(false);

            tokenSource?.Dispose();
        }

        Task task;
        CancellationTokenSource tokenSource;

        static readonly ILog Logger = LogManager.GetLogger<MessagePump>();
    }
}
