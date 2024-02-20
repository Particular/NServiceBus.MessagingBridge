namespace NServiceBus;

public class PassingCustomCheck : CustomCheck
{
    public PassingCustomCheck()
        : base("PassingCheck", "Test", TimeSpan.FromSeconds(1))
    {
    }

    public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CheckResult()
        {
            HasFailed = false
        });
}