namespace NServiceBus.MessagingBridge.CustomChecks;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class TimerBasedPeriodicCheck(
    ICustomCheck check,
    IEnumerable<Func<
        ICustomCheck,
        CheckResult,
        CancellationToken,
        Task>> messageDispatchers)
{
    static readonly ILog Logger = LogManager.GetLogger<TimerBasedPeriodicCheck>();

    CancellationTokenSource stopTokenSource;

    public void Start()
    {
        stopTokenSource = new CancellationTokenSource();

        _ = RunAndSwallowExceptions(stopTokenSource.Token);
    }

    public Task Stop(CancellationToken cancellationToken = default)
    {
        stopTokenSource?.Cancel();

        return Task.CompletedTask;
    }

    async Task RunAndSwallowExceptions(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                try
                {
                    var result = await InvokeAndWrapFailure(check, cancellationToken).ConfigureAwait(false);

                    var sendMessageTasks = new List<Task>();

                    foreach (var messageDispatcher in messageDispatchers)
                    {
                        try
                        {
                            var messageTask = messageDispatcher?.Invoke(check, result, cancellationToken);

                            if (messageTask is not null)
                            {
                                sendMessageTasks.Add(messageTask);
                            }
                        }
                        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                        {
                            Logger.Warn("Failed to send periodic check to ServiceControl.", ex);
                        }
                    }

                    await Task.WhenAll(sendMessageTasks).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    Logger.Error("Custom check failed but can be retried.", ex);
                }

                if (!check.Interval.HasValue)
                {
                    break;
                }

                await Task.Delay(check.Interval.Value, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                // private token, check is being stopped, log the exception in case the stack trace is ever needed for debugging
                Logger.Debug("Operation canceled while stopping custom check.", ex);
                break;
            }
            catch (Exception ex)
            {
                Logger.Error("Custom check failed and cannot be retried.", ex);
                break;
            }
        }
    }

    static async Task<CheckResult> InvokeAndWrapFailure(ICustomCheck check, CancellationToken cancellationToken)
    {
        try
        {
            return await check.PerformCheck(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            var reason = $"'{check.GetType()}' implementation failed to run.";
            Logger.Error(reason, ex);
            return CheckResult.Failed(reason);
        }
    }
}