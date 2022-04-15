using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

public class BridgeConfigurationTests
{
    [Test]
    public void At_least_two_transports_should_be_configured()
    {
        var configuration = new BridgeConfiguration();

        Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        configuration.AddTransport(new BridgeTransport(new SomeTransport()));

        var ex = Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        StringAssert.Contains("At least two", ex.Message);
    }

    [Test]
    public void At_least_on_endpoint_per_endpoint_should_be_configured()
    {
        var configuration = new BridgeConfiguration();

        configuration.AddTransport(new BridgeTransport(new SomeTransport()));
        configuration.AddTransport(new BridgeTransport(new SomeOtherTransport()));

        var ex = Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        StringAssert.Contains("At least one", ex.Message);
        StringAssert.Contains("some, someother", ex.Message);
    }


    [Test]
    public void Should_default_the_endpoint_address_to_the_transport_default()
    {
        var transportAddress = "SomeEndpointAddress";
        var transport = new BridgeTransport(new SomeTransport
        {
            AddresTranslation = _ => transportAddress
        });

        transport.HasEndpoint("SomeEndpoint");

        Assert.AreEqual(transportAddress, transport.Endpoints.Single().QueueAddress);
    }
    [Test]
    public void It_shouldnt_be_allowed_to_shovel_the_error_queue_of_the_bridge()
    {
        var configuration = new BridgeConfiguration();
        var bridgeErrorQueue = "my-error";
        var transport = new BridgeTransport(new SomeTransport())
        {
            ErrorQueue = bridgeErrorQueue
        };

        transport.HasEndpoint("SomeEndpoint");
        transport.HasEndpoint(bridgeErrorQueue);
        configuration.AddTransport(transport);

        var someOtherTransport = new BridgeTransport(new SomeOtherTransport());

        someOtherTransport.HasEndpoint("SomeOtherEndpoint");
        configuration.AddTransport(someOtherTransport);
        var ex = Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        StringAssert.Contains("It is not allowed to register the bridge error queue as an endpoint, please change the error queue or remove the endpoint mapping", ex.Message);
        StringAssert.Contains(bridgeErrorQueue, ex.Message);
        StringAssert.Contains(transport.Name, ex.Message);
    }

    [Test]
    public void Endpoints_should_only_be_added_to_one_transport()
    {
        var duplicatedEndpointName = "DuplicatedEndpoint";
        var configuration = new BridgeConfiguration();

        var someTransport = new BridgeTransport(new SomeTransport());

        someTransport.HasEndpoint(duplicatedEndpointName);
        configuration.AddTransport(someTransport);

        var someOtherTransport = new BridgeTransport(new SomeOtherTransport());

        someOtherTransport.HasEndpoint(duplicatedEndpointName);
        configuration.AddTransport(someOtherTransport);

        var ex = Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        StringAssert.Contains("Endpoints can only be associated with a single transport", ex.Message);
        StringAssert.Contains(duplicatedEndpointName, ex.Message);
    }

    [Test]
    public void Publisher_endpoints_should_be_registered_for_all_subscriptions()
    {
        var configuration = new BridgeConfiguration();

        var someTransport = new BridgeTransport(new SomeTransport());

        someTransport.HasEndpoint("Publisher");
        configuration.AddTransport(someTransport);

        var someOtherTransport = new BridgeTransport(new SomeOtherTransport());

        var subscriber = new BridgeEndpoint("Subscriber");

        subscriber.RegisterPublisher<MyEvent>("NotThePublisher");
        someOtherTransport.HasEndpoint(subscriber);
        configuration.AddTransport(someOtherTransport);

        var ex = Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        StringAssert.Contains("Publisher not registered for", ex.Message);
        StringAssert.Contains(typeof(MyEvent).FullName, ex.Message);
        StringAssert.Contains("NotThePublisher", ex.Message);
    }

    [Test]
    public void Subscriptions_for_same_event_should_have_matching_publisher()
    {
        var configuration = new BridgeConfiguration();

        var someTransport = new BridgeTransport(new SomeTransport());

        someTransport.HasEndpoint("Publisher");
        someTransport.HasEndpoint("OtherEndpoint");
        configuration.AddTransport(someTransport);

        var someOtherTransport = new BridgeTransport(new SomeOtherTransport());

        var subscriber1 = new BridgeEndpoint("Subscriber1");

        subscriber1.RegisterPublisher<MyEvent>("Publisher");
        someOtherTransport.HasEndpoint(subscriber1);

        var subscriber2 = new BridgeEndpoint("Subscriber2");

        subscriber1.RegisterPublisher<MyEvent>("OtherEndpoint");
        someOtherTransport.HasEndpoint(subscriber2);
        configuration.AddTransport(someOtherTransport);

        var ex = Assert.Throws<InvalidOperationException>(() => FinalizeConfiguration(configuration));

        StringAssert.Contains("Events can only be associated with a single publisher", ex.Message);
        StringAssert.Contains(typeof(MyEvent).FullName, ex.Message);
        StringAssert.Contains("Publisher", ex.Message);
        StringAssert.Contains("OtherEndpoint", ex.Message);
    }

    [Test]
    public void Should_require_transports_of_the_same_type_to_be_uniquely_identifiable_by_name()
    {
        var configuration = new BridgeConfiguration();

        configuration.AddTransport(new BridgeTransport(new SomeTransport())
        {
            Name = "some1"
        });
        configuration.AddTransport(new BridgeTransport(new SomeTransport())
        {
            Name = "some2"
        });
        configuration.AddTransport(new BridgeTransport(new SomeTransport()));

        Assert.Throws<InvalidOperationException>(() => configuration.AddTransport(new BridgeTransport(new SomeTransport())));
    }

    [Test]
    public void Should_default_to_transaction_scope_mode_if_all_transports_supports_it()
    {
        var configuration = new BridgeConfiguration();

        var someScopeTransport = new BridgeTransport(new SomeScopeSupportingTransport());

        someScopeTransport.HasEndpoint("Sales");
        configuration.AddTransport(someScopeTransport);


        var someOtherScopeTransport = new BridgeTransport(new SomeOtherScopeSupportingTransport());

        someOtherScopeTransport.HasEndpoint("Billing");
        configuration.AddTransport(someOtherScopeTransport);

        var finalizedConfiguration = FinalizeConfiguration(configuration);

        Assert.False(finalizedConfiguration.TransportConfigurations.Any(tc => tc.TransportDefinition
            .TransportTransactionMode != TransportTransactionMode.TransactionScope));
    }

    [Test]
    public void Should_default_to_receive_only_mode_if_any_of_the_transports_doesnt_support_transaction_scopes()
    {
        var configuration = new BridgeConfiguration();

        var someScopeTransport = new BridgeTransport(new SomeScopeSupportingTransport());

        someScopeTransport.HasEndpoint("Sales");
        configuration.AddTransport(someScopeTransport);


        var someOtherScopeTransport = new BridgeTransport(new SomeOtherTransport());

        someOtherScopeTransport.HasEndpoint("Billing");
        configuration.AddTransport(someOtherScopeTransport);

        var finalizedConfiguration = FinalizeConfiguration(configuration);

        Assert.False(finalizedConfiguration.TransportConfigurations.Any(tc => tc.TransportDefinition
            .TransportTransactionMode != TransportTransactionMode.ReceiveOnly));
    }

    [Test]
    public void Should_allow_receive_only_mode_to_be_configured_even_if_all_transports_support_scopes()
    {
        var configuration = new BridgeConfiguration();

        var someScopeTransport = new BridgeTransport(new SomeScopeSupportingTransport());

        someScopeTransport.HasEndpoint("Sales");
        configuration.AddTransport(someScopeTransport);


        var someOtherScopeTransport = new BridgeTransport(new SomeOtherScopeSupportingTransport());

        someOtherScopeTransport.HasEndpoint("Billing");
        configuration.AddTransport(someOtherScopeTransport);

        configuration.RunInReceiveOnlyTransactionMode();

        var finalizedConfiguration = FinalizeConfiguration(configuration);

        Assert.False(finalizedConfiguration.TransportConfigurations.Any(tc => tc.TransportDefinition
            .TransportTransactionMode != TransportTransactionMode.ReceiveOnly));
    }

    [Test, Ignore("There is currently no way to know if the default was changed by the user")]
    public void Should_throw_if_users_tries_to_set_transaction_mode_on_individual_transports()
    {
    }

    FinalizedBridgeConfiguration FinalizeConfiguration(BridgeConfiguration bridgeConfiguration)
    {
        return bridgeConfiguration.FinalizeConfiguration(new NullLogger<BridgeConfiguration>());
    }

    class SomeScopeSupportingTransport : FakeTransport
    {
        public SomeScopeSupportingTransport() : base(TransportTransactionMode.TransactionScope) { }
    }

    class SomeOtherScopeSupportingTransport : FakeTransport
    {
        public SomeOtherScopeSupportingTransport() : base(TransportTransactionMode.TransactionScope) { }
    }

    class SomeTransport : FakeTransport
    {
        public Func<string, string> AddresTranslation = name => name;
#pragma warning disable CS0672 // Member overrides obsolete member
        public override string ToTransportAddress(QueueAddress address)
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            return AddresTranslation(address.BaseAddress);
        }
    }
    class SomeOtherTransport : FakeTransport { }

    class MyEvent
    {
    }
}

