using System;
using NServiceBus;
using NUnit.Framework;

public class BridgeConfigurationTests
{
    [Test]
    public void Should_require_transports_of_the_same_type_to_be_uniquely_identifiable_by_name()
    {
        var configuration = new BridgeConfiguration();

        configuration.AddTransport(new SomeTransport(), "some1");
        configuration.AddTransport(new SomeTransport(), "some2");

        configuration.AddTransport(new SomeOtherTransport());

        Assert.Throws<InvalidOperationException>(() => configuration.AddTransport(new SomeOtherTransport()));
    }

    class SomeTransport : FakeTransport { }
    class SomeOtherTransport : FakeTransport { }
}