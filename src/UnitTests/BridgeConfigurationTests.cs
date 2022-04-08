using System;
using NServiceBus;
using NUnit.Framework;

public class BridgeConfigurationTests
{
    [Test]
    public void At_least_two_transports_should_be_configured()
    {
        var configuration = new BridgeConfiguration();

        Assert.Throws<InvalidOperationException>(() => configuration.Validate());

        configuration.AddTransport(new BridgeTransportConfiguration(new SomeTransport()));

        Assert.Throws<InvalidOperationException>(() => configuration.Validate());

        configuration.AddTransport(new BridgeTransportConfiguration(new SomeOtherTransport()));

        configuration.Validate();
    }

    [Test]
    public void Should_require_transports_of_the_same_type_to_be_uniquely_identifiable_by_name()
    {
        var configuration = new BridgeConfiguration();

        configuration.AddTransport(new BridgeTransportConfiguration(new SomeTransport())
        {
            Name = "some1"
        });
        configuration.AddTransport(new BridgeTransportConfiguration(new SomeTransport())
        {
            Name = "some2"
        });
        configuration.AddTransport(new BridgeTransportConfiguration(new SomeTransport()));

        Assert.Throws<InvalidOperationException>(() => configuration.AddTransport(new BridgeTransportConfiguration(new SomeTransport())));
    }

    class SomeTransport : FakeTransport { }
    class SomeOtherTransport : FakeTransport { }
}

