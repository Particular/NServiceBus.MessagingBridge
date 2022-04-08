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
        endpoint.RegisterPublisher("Shipping", typeof(MyOtherEvent));

        var billingSubscription = endpoint.Subscriptions.SingleOrDefault(t => t.EventTypeFullName == typeof(MyEvent).FullName);
        Assert.AreEqual("Billing", billingSubscription.Publisher);

        var shippingSubscription = endpoint.Subscriptions.SingleOrDefault(t => t.EventTypeFullName == typeof(MyOtherEvent).FullName);
        Assert.AreEqual("Shipping", shippingSubscription.Publisher);

    }

    class MyEvent
    {
    }

    class MyOtherEvent
    {
    }
}

