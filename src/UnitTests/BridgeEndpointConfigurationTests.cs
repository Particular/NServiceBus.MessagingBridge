using System.Linq;
using NServiceBus;
using NUnit.Framework;

public class BridgeEndpointConfigurationTests
{
    [Test]
    public void Should_allow_subscribing_by_type()
    {
        var endpoint = new BridgeEndpoint("Sales");

        endpoint.RegisterPublisher<MyEvent>("Billing");

        Assert.AreEqual(endpoint.Subscriptions.Single().EventTypeFullName, typeof(MyEvent).FullName);
        Assert.AreEqual(endpoint.Subscriptions.Single().Publisher, "Billing");
    }

    class MyEvent
    {
    }
}

