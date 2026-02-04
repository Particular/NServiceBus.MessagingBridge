using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class MassTransitAdapter : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_send_message_through_bridge_with_masstransit_adapter_configured()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                // ServiceControl side - using default test transport
                var serviceControlTransport = DefaultTestServer.GetTestTransportDefinition().ToTestableBridge("ServiceControlTransport");
                serviceControlTransport.AddTestEndpoint<ServiceControlEndpoint>();
                bridgeConfiguration.AddTransport(serviceControlTransport);

                // MassTransit side - configure with adapter
                var mtEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(MassTransitEndpoint)));
                mtEndpoint.UseMessageFormat(new MassTransitFormatAdapter());
                transportBeingTested.HasEndpoint(mtEndpoint);
                bridgeConfiguration.AddTransport(transportBeingTested);
            })
            .WithEndpoint<MassTransitEndpoint>(b => b
                .When(c => c.EndpointsStarted, (session, _) =>
                {
                    var options = new SendOptions();
                    options.SetDestination(Conventions.EndpointNamingConvention(typeof(ServiceControlEndpoint)));
                    return session.Send(new TestMessage { Content = "Test" }, options);
                }))
            .WithEndpoint<ServiceControlEndpoint>()
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(context.MessageReceived, Is.True, "Message should be received by ServiceControl endpoint");
        Assert.That(context.ReceivedContent, Is.EqualTo("Test"), "Message content should be preserved");
    }

    public class MassTransitEndpoint : EndpointConfigurationBuilder
    {
        public MassTransitEndpoint() => EndpointSetup<DefaultServer>();
    }

    public class ServiceControlEndpoint : EndpointConfigurationBuilder
    {
        public ServiceControlEndpoint() => EndpointSetup<DefaultTestServer>();

        public class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                testContext.ReceivedContent = message.Content;
                return Task.CompletedTask;
            }
        }
    }

    public class Context : BridgeScenarioContext
    {
        public bool MessageReceived { get; set; }
        public string ReceivedContent { get; set; }
    }

    public class TestMessage : IMessage
    {
        public string Content { get; set; }
    }
}
