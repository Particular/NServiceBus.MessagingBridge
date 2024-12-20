using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ThreeTransports : BridgeAcceptanceTest
{
    [Test(Description = "Replicates issue reported in https://github.com/Particular/NServiceBus.MessagingBridge/issues/369")]
    public async Task Should_translate_address_correctly_for_target_transport()
    {
        var endpointOnTestingTransportName = Conventions.EndpointNamingConvention(typeof(EndpointOnTestingTransport));
        var endpointOnTransportUnderTestName = Conventions.EndpointNamingConvention(typeof(EndpointOnTransportUnderTest));

        var options = new SendOptions();
        options.SetDestination(Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)));

        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>()
            .WithEndpoint<EndpointOnTestingTransport>(builder => builder
                .When(c => c.EndpointsStarted, (session, _) => session.Send(new SomeMessage { From = endpointOnTestingTransportName }, options)))
            .WithEndpoint<EndpointOnTransportUnderTest>(builder => builder
                .When(c => c.EndpointsStarted, (session, _) => session.Send(new SomeMessage { From = endpointOnTransportUnderTestName }, options)))
            .WithBridge(bridgeConfiguration =>
            {
                bridgeConfiguration.TranslateReplyToAddressForFailedMessages();

                var receivingTransport = new TestableBridgeTransport(ReceivingTestServer.GetReceivingTransportDefinition())
                {
                    Name = "ReceivingTransport"
                };
                receivingTransport.AddTestEndpoint<ReceivingEndpoint>();
                bridgeConfiguration.AddTransport(receivingTransport);

                var acceptanceTestingTransport = new TestableBridgeTransport(SendingTestServer.GetSendingTransportDefinition())
                {
                    Name = "SendingAcceptanceTestingTransportName"
                };
                acceptanceTestingTransport.AddTestEndpoint<EndpointOnTestingTransport>();
                bridgeConfiguration.AddTransport(acceptanceTestingTransport);

                var transportUnderTest = new TestableBridgeTransport(TransportBeingTested)
                {
                    Name = "TransportUnderTest"
                };
                transportUnderTest.AddTestEndpoint<EndpointOnTransportUnderTest>();
                bridgeConfiguration.AddTransport(transportUnderTest);
            })
            .Done(c => c.ReceivedMessageCount == 2)
            .Run();

    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<ReceivingTestServer>();

        class SomeMessageHandler : IHandleMessages<SomeMessage>
        {
            public SomeMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(context.MessageHeaders.TryGetValue("NServiceBus.ReplyToAddress", out var headerValue), Is.True);
                    Assert.That(headerValue, Is.EqualTo(message.From));
                });

                testContext.ReceivedMessageCount++;

                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class EndpointOnTestingTransport : EndpointConfigurationBuilder
    {
        public EndpointOnTestingTransport() => EndpointSetup<SendingTestServer>();
    }

    public class EndpointOnTransportUnderTest : EndpointConfigurationBuilder
    {
        public EndpointOnTransportUnderTest() => EndpointSetup<DefaultServer>();
    }

    public class Context : ScenarioContext
    {
        public int ReceivedMessageCount { get; set; }
    }

    public class SomeMessage : IMessage
    {
        public string From { get; set; }
    }
}