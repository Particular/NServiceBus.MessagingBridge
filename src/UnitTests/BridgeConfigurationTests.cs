using System;
using System.Linq;
using NServiceBus;
using NUnit.Framework;

public class BridgeConfigurationTests
{
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

    [Test]
    public void Should_allow_subscribing_by_type()
    {
        var endpoint = new BridgeEndpoint("Sales");

        endpoint.RegisterPublisher<MyEvent>("Billing");

        Assert.AreEqual(endpoint.Subscriptions.Single().EventTypeFullName, typeof(MyEvent).FullName);
        Assert.AreEqual(endpoint.Subscriptions.Single().Publisher, "Billing");
    }

    class SomeTransport : FakeTransport { }
    class SomeOtherTransport : FakeTransport { }
    class MyEvent
    {
    }
}

