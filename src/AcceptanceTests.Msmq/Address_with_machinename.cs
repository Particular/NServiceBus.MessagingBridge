using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Address_with_machinename : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_get_the_message()
    {
        var ctx = await Scenario.Define<Context>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                var receivingEndpointName =
                    Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint));

                var receivingEndpoint =
                    new BridgeEndpoint(receivingEndpointName, receivingEndpointName + "@localhost");

                bridgeTransport.HasEndpoint(receivingEndpoint);

                bridgeConfiguration.AddTransport(bridgeTransport);

                bridgeConfiguration.AddTestTransportEndpoint<SendingEndpoint>();
            })
            .WithEndpoint<SendingEndpoint>(c => c
                .When(cc => cc.EndpointsStarted, (b, _) => b.Send(new MyMessage())))
            .WithEndpoint<ReceivingEndpoint>()
            .Done(c => c.ReceivingEndpointGotMessage)
            .Run();

        Assert.That(ctx.ReceivingEndpointGotMessage, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool ReceivingEndpointGotMessage { get; set; }
    }

    class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() =>
            EndpointSetup<DefaultTestServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(ReceivingEndpoint));
            });
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultServer>();

        public class MessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivingEndpointGotMessage = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}