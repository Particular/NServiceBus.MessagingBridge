using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Send : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_only_arrive_once()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(c => c
                .When(b => b.Send(new MyMessage())))
            .WithEndpoint<ReceivingEndpoint>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransportConfiguration = new BridgeTransportConfiguration(TransportBeingTested);

                bridgeTransportConfiguration.AddTestEndpoint<SendingEndpoint>();
                bridgeConfiguration.AddTransport(bridgeTransportConfiguration);

                var receivingEndpointConfiguration =
                    new BridgeTransportConfiguration(DefaultTestServer.GetTestTransportDefinition())
                    {
                        Name = "SomeOtherTestingTransport"
                    };

                receivingEndpointConfiguration.HasEndpoint(
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint))));
                receivingEndpointConfiguration.HasEndpoint(
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(AdditionalReceivingEndpoint))));

                bridgeConfiguration.AddTransport(receivingEndpointConfiguration);
            })
            .Done(c => c.MessagesReceived == 2)
            .Run().ConfigureAwait(false);

        Assert.IsTrue(ctx.MessagesReceived == 1, "Messages arrived: {0}", ctx.MessagesReceived);
    }

    public class Context : ScenarioContext
    {
        public int MessagesReceived { get; set; }
    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage),
                    Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)));
            });
        }
    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultTestServer>();

        public class MessageHandler : IHandleMessages<MyMessage>
        {
            public MessageHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MessagesReceived++;
                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class AdditionalReceivingEndpoint : EndpointConfigurationBuilder
    {
        public AdditionalReceivingEndpoint() => EndpointSetup<DefaultTestServer>();
    }

    public class MyMessage : IMessage
    {
    }
}