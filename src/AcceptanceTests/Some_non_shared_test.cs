using System.Threading.Tasks;
using NUnit.Framework;

public class Some_non_shared_test : RouterAcceptanceTest
{
    [Test]
    public Task Blah()
    {
        return Task.CompletedTask;
    }
}