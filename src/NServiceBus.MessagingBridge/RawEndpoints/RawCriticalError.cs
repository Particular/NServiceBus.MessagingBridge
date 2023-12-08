namespace NServiceBus.Raw
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    class RawCriticalError : CriticalError
    {
        public RawCriticalError(Func<ICriticalErrorContext, CancellationToken, Task> onCriticalErrorAction)
            : base(onCriticalErrorAction) =>
            criticalErrorAction = onCriticalErrorAction;

        public override void Raise(string errorMessage, Exception exception, CancellationToken cancellationToken = default)
        {
            //Intentionally don't call base.Raise
            ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
            ArgumentNullException.ThrowIfNull(exception);

            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            lock (endpointCriticalLock)
            {
                if (endpoint == null)
                {
                    criticalErrors.Add(new LatentCritical
                    {
                        Message = errorMessage,
                        Exception = exception
                    });
                    return;
                }
            }

            // don't await the criticalErrorAction in order to avoid deadlocks
            RaiseForEndpoint(errorMessage, exception, cancellationToken);
        }

        void RaiseForEndpoint(string errorMessage, Exception exception, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(errorMessage);
            ArgumentNullException.ThrowIfNull(exception);

            _ = Task.Run(() =>
              {
                  var context = new CriticalErrorContext(async (_) =>
                  {
                      var stoppable = await endpoint.StopReceiving(cancellationToken).ConfigureAwait(false);
                      await stoppable.Stop(cancellationToken).ConfigureAwait(false);
                  }, errorMessage, exception);
                  return criticalErrorAction(context, cancellationToken);
              }, cancellationToken);
        }

        internal void SetEndpoint(IReceivingRawEndpoint endpointInstance, CancellationToken cancellationToken = default)
        {
            lock (endpointCriticalLock)
            {
                endpoint = endpointInstance;
                foreach (var latentCritical in criticalErrors)
                {
                    RaiseForEndpoint(latentCritical.Message, latentCritical.Exception, cancellationToken);
                }
                criticalErrors.Clear();
            }
        }

        IReceivingRawEndpoint endpoint;

        readonly Func<CriticalErrorContext, CancellationToken, Task> criticalErrorAction;
        readonly List<LatentCritical> criticalErrors = [];
        readonly object endpointCriticalLock = new object();

        class LatentCritical
        {
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}