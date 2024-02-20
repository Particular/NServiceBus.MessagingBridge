namespace NServiceBus;

public class PassingCustomCheck : CustomCheck
{
    public PassingCustomCheck()
        : base("Passing Check", "Test", TimeSpan.FromSeconds(1))
    {
    }

    public override Task<CheckResult> PerformCheck(CancellationToken cancellationToken = default) => CheckResult.Failed("Testing");
}