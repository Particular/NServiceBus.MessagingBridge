namespace NServiceBus.MessagingBridge.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using MSMQ.Messaging;
    using Support;
    using Transport;

    class MessagePump : IMessageReceiver
    {
        public MessagePump(
            Func<TransportTransactionMode, ReceiveStrategy> receiveStrategyFactory,
            TimeSpan messageEnumeratorTimeout,
            TransportTransactionMode transactionMode,
            bool ignoreIncomingTimeToBeReceivedHeaders,
            Action<string, Exception, CancellationToken> criticalErrorAction,
            ReceiveSettings receiveSettings)
        {
            this.receiveStrategyFactory = receiveStrategyFactory;
            this.messageEnumeratorTimeout = messageEnumeratorTimeout;
            this.transactionMode = transactionMode;
            this.ignoreIncomingTimeToBeReceivedHeaders = ignoreIncomingTimeToBeReceivedHeaders;
            this.criticalErrorAction = criticalErrorAction;
            this.receiveSettings = receiveSettings;

            ReceiveAddress = MsmqTransportInfrastructure.TranslateAddress(receiveSettings.ReceiveAddress);
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
        {
            var inputAddress = MsmqAddress.Parse(ReceiveAddress);
            var errorAddress = MsmqAddress.Parse(receiveSettings.ErrorQueue);

            if (!string.Equals(inputAddress.Machine, RuntimeEnvironment.MachineName,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    $"MSMQ Dequeuing can only run against the local machine. Invalid inputQueue name '{receiveSettings.ReceiveAddress}'.");
            }

            inputQueue = new MessageQueue(inputAddress.FullPath, false, true, QueueAccessMode.Receive);
            errorQueue = new MessageQueue(errorAddress.FullPath, false, true, QueueAccessMode.Send);

            if (transactionMode != TransportTransactionMode.None && !QueueIsTransactional())
            {
                throw new ArgumentException(
                    $"Queue must be transactional if you configure the endpoint to be transactional ({receiveSettings.ReceiveAddress}).");
            }

            inputQueue.MessageReadPropertyFilter = DefaultReadPropertyFilter;

            if (receiveSettings.PurgeOnStartup)
            {
                inputQueue.Purge();
            }

            receiveStrategy = receiveStrategyFactory(transactionMode);
            receiveStrategy.Init(inputQueue, ReceiveAddress, errorQueue, onMessage, onError, criticalErrorAction, ignoreIncomingTimeToBeReceivedHeaders);

            maxConcurrency = limitations.MaxConcurrency;
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency, limitations.MaxConcurrency);
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = default)
        {
            MessageQueue.ClearConnectionCache();

            messagePumpCancellationTokenSource = new CancellationTokenSource();
            messageProcessingCancellationTokenSource = new CancellationTokenSource();

            peekCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("MsmqPeek", TimeSpan.FromSeconds(30),
                ex => criticalErrorAction("Failed to peek " + receiveSettings.ReceiveAddress, ex, messageProcessingCancellationTokenSource.Token));

            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("MsmqReceive", TimeSpan.FromSeconds(30),
                ex => criticalErrorAction("Failed to receive from " + receiveSettings.ReceiveAddress, ex, messageProcessingCancellationTokenSource.Token));

            // Task.Run() so the call returns immediately instead of waiting for the first await or return down the call stack
            // LongRunning is useless combined with async/await
            messagePumpTask = Task.Run(() => PumpMessagesAndSwallowExceptions(messagePumpCancellationTokenSource.Token), CancellationToken.None);

            return Task.CompletedTask;
        }

        public async Task ChangeConcurrency(PushRuntimeSettings newLimitations, CancellationToken cancellationToken = default)
        {
            var oldLimiter = concurrencyLimiter;
            var oldMaxConcurrency = maxConcurrency;
            concurrencyLimiter = new SemaphoreSlim(newLimitations.MaxConcurrency, newLimitations.MaxConcurrency);
            maxConcurrency = newLimitations.MaxConcurrency;

            try
            {
                //Drain and dispose of the old semaphore
                while (oldLimiter.CurrentCount != oldMaxConcurrency)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
                oldLimiter.Dispose();
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                //Ignore, we are stopping anyway
            }
        }

        public async Task StopReceive(CancellationToken cancellationToken = default)
        {
            messagePumpCancellationTokenSource?.Cancel();

            using (cancellationToken.Register(() => messageProcessingCancellationTokenSource?.Cancel()))
            {
                await messagePumpTask.ConfigureAwait(false);

                while (concurrencyLimiter.CurrentCount != maxConcurrency)
                {
                    // We are deliberately not forwarding the cancellation token here because
                    // this loop is our way of waiting for all pending messaging operations
                    // to participate in cooperative cancellation or not.
                    // We do not want to rudely abort them because the cancellation token has been canceled.
                    // This allows us to preserve the same behaviour in v8 as in v7 in that,
                    // if CancellationToken.None is passed to this method,
                    // the method will only return when all in flight messages have been processed.
                    // If, on the other hand, a non-default CancellationToken is passed,
                    // all message processing operations have the opportunity to
                    // participate in cooperative cancellation.
                    // If we ever require a method of stopping the endpoint such that
                    // all message processing is canceled immediately,
                    // we can provide that as a separate feature.
                    await Task.Delay(50, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }

            concurrencyLimiter?.Dispose();
            inputQueue?.Dispose();
            errorQueue?.Dispose();
            peekCircuitBreaker?.Dispose();
            receiveCircuitBreaker?.Dispose();
            messagePumpCancellationTokenSource?.Dispose();
            messageProcessingCancellationTokenSource?.Dispose();
        }

        [DebuggerNonUserCode]
        async Task PumpMessagesAndSwallowExceptions(CancellationToken messagePumpCancellationToken)
        {
            while (!messagePumpCancellationToken.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        await PumpMessages(messagePumpCancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!ex.IsCausedBy(messagePumpCancellationToken))
                    {
                        Logger.Error("MSMQ Message pump failed", ex);
                        await peekCircuitBreaker.Failure(ex, messagePumpCancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex.IsCausedBy(messagePumpCancellationToken))
                {
                    // private token, sender is being stopped, log the exception in case the stack trace is ever needed for debugging
                    Logger.Debug("Operation canceled while stopping message pump.", ex);
                    break;
                }
            }
        }

        async Task PumpMessages(CancellationToken messagePumpCancellationToken)
        {
            using (var enumerator = inputQueue.GetMessageEnumerator2())
            {
                while (true)
                {
                    messagePumpCancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        //note: .Peek will throw an ex if no message is available. It also turns out that .MoveNext is faster since message isn't read
                        if (!enumerator.MoveNext(messageEnumeratorTimeout))
                        {
                            continue;
                        }

                        peekCircuitBreaker.Success();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("MSMQ receive operation failed", ex);
                        await peekCircuitBreaker.Failure(ex, messagePumpCancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    messagePumpCancellationToken.ThrowIfCancellationRequested();

                    var localLimiter = concurrencyLimiter;

                    await localLimiter.WaitAsync(messagePumpCancellationToken).ConfigureAwait(false);

                    _ = Task.Factory.StartNew(state =>
                        {
                            var (messagePump, limiter, cancellationToken) = ((MessagePump, SemaphoreSlim, CancellationToken))state;
                            return ReceiveMessagesSwallowExceptionsAndReleaseConcurrencyLimiter(messagePump, limiter, cancellationToken);
                        },
                        (this, localLimiter, messagePumpCancellationToken),  // We pass a state to make sure we benefit from lamda delegate caching. See https://github.com/Particular/NServiceBus/issues/3884
                        CancellationToken.None,  // CancellationToken.None is used here since cancelling the task before it can run can cause the concurrencyLimiter to not be released
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default)
                        .Unwrap();
                }
            }
        }

        // This is static to prevent the method from accessing fields in the pump since that causes variable capturing and cause extra allocations
        static async Task ReceiveMessagesSwallowExceptionsAndReleaseConcurrencyLimiter(MessagePump messagePump, SemaphoreSlim localConcurrencyLimiter, CancellationToken messagePumpCancellationToken)
        {
#pragma warning disable PS0021 // Highlight when a try block passes multiple cancellation tokens - justification:
            // The message processing cancellation token is being used for the receive strategies,
            // since we want those only to be canceled when the public token passed to Stop() is canceled.
            // The message pump token is being used elsewhere, because we want those operations to be canceled as soon as Stop() is called.
            // The catch clauses on the inner try are correctly filtered on the message processing cancellation token and
            // the catch clause on the outer try is correctly filtered on the message pump cancellation token.
            try
#pragma warning restore PS0021 // Highlight when a try block passes multiple cancellation tokens
            {
                try
                {
                    // we are switching token here so we need to catch cancellation
                    await messagePump.receiveStrategy.ReceiveMessage(messagePump.messageProcessingCancellationTokenSource.Token).ConfigureAwait(false);
                    messagePump.receiveCircuitBreaker.Success();
                }
                catch (Exception ex) when (ex.IsCausedBy(messagePump.messageProcessingCancellationTokenSource.Token))
                {
                    Logger.Warn("MSMQ receive operation canceled", ex);
                }
                catch (Exception ex)
                {
                    Logger.Warn("MSMQ receive operation failed", ex);
                    await messagePump.receiveCircuitBreaker.Failure(ex, messagePumpCancellationToken).ConfigureAwait(false);
                }
            }
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - justification: see PS0021 suppression justification
            catch (Exception ex) when (ex.IsCausedBy(messagePumpCancellationToken))
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            {
                // private token, sender is being stopped, log the exception in case the stack trace is ever needed for debugging
                Logger.Debug("Operation canceled while stopping message pump.", ex);
            }
            finally
            {
                localConcurrencyLimiter.Release();
            }
        }

        bool QueueIsTransactional()
        {
            try
            {
                return inputQueue.Transactional;
            }
            catch (MessageQueueException msmqEx)
            {
                var error =
                    $"There is a problem with the input inputQueue: {inputQueue.Path}. See the enclosed exception for details.";
                if (msmqEx.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                {
                    error =
                        $"The queue {inputQueue.Path} does not exist. Run the CreateQueues.ps1 script included in the project output, or enable queue creation on startup using EndpointConfiguration.EnableInstallers().";
                }

                if (msmqEx.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    error =
                        $"Access denied for the queue {inputQueue.Path}. Ensure the user has Get Properties permission on the queue.";
                }

                throw new Exception(error, msmqEx);
            }
            catch (Exception ex)
            {
                var error =
                    $"There is a problem with the input inputQueue: {inputQueue.Path}. See the enclosed exception for details.";
                throw new Exception(error, ex);
            }
        }

        public ISubscriptionManager Subscriptions => null;

        public string Id => receiveSettings.Id;

        public string ReceiveAddress { get; }

        CancellationTokenSource messagePumpCancellationTokenSource;
        CancellationTokenSource messageProcessingCancellationTokenSource;
        int maxConcurrency;
        volatile SemaphoreSlim concurrencyLimiter;
        MessageQueue errorQueue;
        MessageQueue inputQueue;

        Task messagePumpTask;

        ReceiveStrategy receiveStrategy;

        RepeatedFailuresOverTimeCircuitBreaker peekCircuitBreaker;
        RepeatedFailuresOverTimeCircuitBreaker receiveCircuitBreaker;
        Func<TransportTransactionMode, ReceiveStrategy> receiveStrategyFactory;
        TimeSpan messageEnumeratorTimeout;
        readonly TransportTransactionMode transactionMode;
        readonly bool ignoreIncomingTimeToBeReceivedHeaders;
        readonly Action<string, Exception, CancellationToken> criticalErrorAction;
        readonly ReceiveSettings receiveSettings;

        static readonly ILog Logger = LogManager.GetLogger<MessagePump>();

        static readonly MessagePropertyFilter DefaultReadPropertyFilter = new MessagePropertyFilter
        {
            Body = true,
            TimeToBeReceived = true,
            Recoverable = true,
            Id = true,
            ResponseQueue = true,
            CorrelationId = true,
            Extension = true,
            AppSpecific = true
        };
    }
}
