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

        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                var receivingTransport = ReceivingTestServer.GetReceivingTransportDefinition().ToTestableBridge("ReceivingTransport");
                receivingTransport.AddTestEndpoint<ReceivingEndpoint>();
                bridgeConfiguration.AddTransport(receivingTransport);

                var sendingTransport = SendingTestServer.GetSendingTransportDefinition().ToTestableBridge("SendingTransport");
                sendingTransport.AddTestEndpoint<EndpointOnTestingTransport>();
                bridgeConfiguration.AddTransport(sendingTransport);

                transportBeingTested.AddTestEndpoint<EndpointOnTransportUnderTest>();
                bridgeConfiguration.AddTransport(transportBeingTested);
            })
            .WithEndpoint<ReceivingEndpoint>()
            .WithEndpoint<EndpointOnTestingTransport>(b => b
                .When(c => c.EndpointsStarted, (session, _) => session.Send(new SomeMessage { From = endpointOnTestingTransportName }, options)))
            .WithEndpoint<EndpointOnTransportUnderTest>(b => b
                .When(c => c.EndpointsStarted, (session, _) => session.Send(new SomeMessage { From = endpointOnTransportUnderTestName }, options)))
            .Done(c => c.ReceivedMessageCount == 2)
            .Run();

    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<ReceivingTestServer>();

        class SomeMessageHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
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

    public class Context : BridgeScenarioContext
    {
        public int ReceivedMessageCount { get; set; }
    }

    public class SomeMessage : IMessage
    {
        public string From { get; set; }
    }
}