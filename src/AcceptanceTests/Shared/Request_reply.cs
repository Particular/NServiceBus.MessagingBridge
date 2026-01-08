using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;

public class Request_reply : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_get_the_reply() =>
        await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                transportBeingTested.AddTestEndpoint<SendingEndpoint>();
                bridgeConfiguration.AddTransport(transportBeingTested);

                bridgeConfiguration.AddTestTransportEndpoint<ReplyingEndpoint>();
            })
            .WithEndpoint<SendingEndpoint>(b => b.When(session => session.Send(new MyMessage())))
            .WithEndpoint<ReplyingEndpoint>()
            .Run();

    public class Context : BridgeScenarioContext;

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() =>
            EndpointSetup<DefaultServer>(c => c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(ReplyingEndpoint)));

        public class ResponseHandler(Context testContext) : IHandleMessages<MyReply>
        {
            public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class ReplyingEndpoint : EndpointConfigurationBuilder
    {
        public ReplyingEndpoint() => EndpointSetup<DefaultTestServer>();

        public class MessageHandler : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context) => context.Reply(new MyReply());
        }
    }

    public class MyMessage : IMessage;

    public class MyReply : IMessage;
}