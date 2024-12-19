using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ReplyToAddress : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_translate_address_for_already_migrated_endpoint()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<SendingEndpoint>(builder =>
            {
                builder.DoNotFailOnErrorMessages();
                builder.When(c => c.EndpointsStarted, (session, _) =>
                {
                    var options = new SendOptions();
                    options.SetDestination(Conventions.EndpointNamingConvention(typeof(SecondMigratedEndpoint)));

                    return session.Send(new ADelayedMessage(), options);
                });
            })
            .WithEndpoint<FirstMigratedEndpoint>()
            .WithEndpoint<SecondMigratedEndpoint>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(DefaultTestServer.GetTestTransportDefinition())
                {
                    Name = "DefaultTestingTransport"
                };
                bridgeTransport.AddTestEndpoint<SendingEndpoint>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var theOtherTransport = new TestableBridgeTransport(TransportBeingTested);
                theOtherTransport.AddTestEndpoint<FirstMigratedEndpoint>();
                theOtherTransport.AddTestEndpoint<SecondMigratedEndpoint>();
                bridgeConfiguration.AddTransport(theOtherTransport);
            })
            .Done(c => c.ADelayedMessageReceived)
            .Run();

    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint() => EndpointSetup<DefaultTestServer>((c, runDescriptor) =>
            c.Pipeline.Register(new OverrideReplyToAddress(), "Checks that the retry confirmation arrived"));

        class OverrideReplyToAddress : Behavior<IOutgoingPhysicalMessageContext>
        {
            public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
            {
                context.Headers[Headers.ReplyToAddress] = Conventions.EndpointNamingConvention(typeof(FirstMigratedEndpoint));
                await next();

            }
        }
    }

    public class FirstMigratedEndpoint : EndpointConfigurationBuilder
    {
        public FirstMigratedEndpoint() => EndpointSetup<DefaultServer>();
    }

    public class SecondMigratedEndpoint : EndpointConfigurationBuilder
    {
        public SecondMigratedEndpoint() => EndpointSetup<DefaultServer>();

        class ADelayedMessageHandler : IHandleMessages<ADelayedMessage>
        {
            public ADelayedMessageHandler(Context context) => testContext = context;

            public Task Handle(ADelayedMessage message, IMessageHandlerContext context)
            {
                testContext.ADelayedMessageReceived = true;
                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class Context : ScenarioContext
    {
        public bool ADelayedMessageReceived { get; set; }
    }

    public class ADelayedMessage : IMessage
    {
    }
}