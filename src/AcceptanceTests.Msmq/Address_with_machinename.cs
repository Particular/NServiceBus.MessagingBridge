using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Address_with_machinename : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_get_the_message() =>
        await Scenario.Define<Context>()
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
                .When(session => session.Send(new MyMessage())))
            .WithEndpoint<ReceivingEndpoint>()
            .Run();

    class Context : BridgeScenarioContext;

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
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}