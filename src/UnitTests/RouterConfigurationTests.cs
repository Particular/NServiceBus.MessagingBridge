using System;
using NUnit.Framework;

partial class RouterConfigurationTests
{
    [Test]
    public void Subscriber_should_get_the_event()
    {
        var configuration = new RouterConfiguration();

        configuration.AddTransport(new FakeTransport());

        Assert.Throws<InvalidOperationException>(() => configuration.AddTransport(new FakeTransport()));
    }
}