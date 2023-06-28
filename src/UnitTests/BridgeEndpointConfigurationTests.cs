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
        endpoint.RegisterPublisher(typeof(MyOtherEvent), "Shipping");

        var billingSubscription = endpoint.Subscriptions.SingleOrDefault(t => t.EventTypeAssemblyQualifiedName == typeof(MyEvent).AssemblyQualifiedName);
        Assert.AreEqual("Billing", billingSubscription.Publisher);

        var shippingSubscription = endpoint.Subscriptions.SingleOrDefault(t => t.EventTypeAssemblyQualifiedName == typeof(MyOtherEvent).AssemblyQualifiedName);
        Assert.AreEqual("Shipping", shippingSubscription.Publisher);

    }

    class MyEvent
    {
    }

    class MyOtherEvent
    {
    }
}

