namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    interface IAsyncTimer
    {
        void Start(Func<CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback);
        Task Stop(CancellationToken cancellationToken = default);
    }
}