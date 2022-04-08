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

        var ex = Assert.Throws<InvalidOperationException>(() => configuration.Validate());

        StringAssert.Contains("At least two", ex.Message);
    }

    [Test]
    public void At_least_on_endpoint_per_endpoint_should_be_configured()
    {
        var configuration = new BridgeConfiguration();

        configuration.AddTransport(new BridgeTransportConfiguration(new SomeTransport()));
        configuration.AddTransport(new BridgeTransportConfiguration(new SomeOtherTransport()));

        var ex = Assert.Throws<InvalidOperationException>(() => configuration.Validate());

        StringAssert.Contains("At least one", ex.Message);
        StringAssert.Contains("some, someother", ex.Message);
    }

    [Test]
    public void Endpoints_should_only_be_added_to_one_transport()
    {
        var duplicatedEndpointName = "DuplicatedEndpoint";
        var configuration = new BridgeConfiguration();

        var someTransport = new BridgeTransportConfiguration(new SomeTransport());

        someTransport.HasEndpoint(duplicatedEndpointName);
        configuration.AddTransport(someTransport);

        var someOtherTransport = new BridgeTransportConfiguration(new SomeOtherTransport());

        someOtherTransport.HasEndpoint(duplicatedEndpointName);
        configuration.AddTransport(someOtherTransport);

        var ex = Assert.Throws<InvalidOperationException>(() => configuration.Validate());

        StringAssert.Contains("Endpoints can only be associated with a single transport", ex.Message);
        StringAssert.Contains(duplicatedEndpointName, ex.Message);
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

