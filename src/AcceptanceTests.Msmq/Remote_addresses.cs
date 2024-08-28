using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.Transport;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Remote_addresses : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_get_the_reply()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(c => c
                .When(cc => cc.EndpointsStarted, (b, _) => b.Send(new MyMessage())))
            .WithEndpoint<RemoteEndpoint>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                var receivingEndpointName =
                    Conventions.EndpointNamingConvention(typeof(RemoteEndpoint));

                var receivingEndpoint =
                    new BridgeEndpoint(receivingEndpointName, receivingEndpointName + "@localhost");

                bridgeTransport.HasEndpoint(receivingEndpoint);

                bridgeConfiguration.AddTransport(bridgeTransport);

                bridgeConfiguration.AddTestTransportEndpoint<SendingEndpoint>();
            })
            .Done(c => c.ReceivingEndpointGotMessage)
            .Run();

        Assert.That(ctx.ReceivingEndpointGotMessage, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ReceivingEndpointGotMessage { get; set; }
    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint()
        {
            EndpointSetup<DefaultTestServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(RemoteEndpoint));
            });
        }
    }

    public class RemoteEndpoint : EndpointConfigurationBuilder
    {
        public RemoteEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class MessageHandler : IHandleMessages<MyMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context context)
            {
                testContext = context;
            }
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivingEndpointGotMessage = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}