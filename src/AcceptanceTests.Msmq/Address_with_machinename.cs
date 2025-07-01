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
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                var receivingEndpointName =
                    Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint));

                var receivingEndpoint =
                    new BridgeEndpoint(receivingEndpointName, receivingEndpointName + "@localhost");

                transportBeingTested.HasEndpoint(receivingEndpoint);

                bridgeConfiguration.AddTransport(transportBeingTested);

                bridgeConfiguration.AddTestTransportEndpoint<SendingEndpoint>();
            })
            .WithEndpoint<SendingEndpoint>(b => b
                .When(c => c.EndpointsStarted, (session, _) => session.Send(new MyMessage())))
            .WithEndpoint<ReceivingEndpoint>()
            .Done(c => c.ReceivingEndpointGotMessage)
            .Run();

        Assert.That(ctx.ReceivingEndpointGotMessage, Is.True);
    }

    class Context : BridgeScenarioContext
    {
        public bool ReceivingEndpointGotMessage { get; set; }
    }

    class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() =>
            EndpointSetup<DefaultTestServer>(b => b.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(ReceivingEndpoint)));
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