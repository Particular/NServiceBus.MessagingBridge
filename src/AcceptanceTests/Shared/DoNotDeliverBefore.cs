using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class DoNotDeliverBefore : BridgeAcceptanceTest
{
    static string OriginalEndpointName = Conventions.EndpointNamingConvention(typeof(OriginalEndpoint));

    [Test]
    public async Task Should_set_TTBR_correctly()
    {
        var ctx = await Scenario.Define<Context>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(DefaultTestServer.GetTestTransportDefinition())
                {
                    Name = "DefaultTestingTransport"
                };
                bridgeTransport.AddTestEndpoint<OriginalEndpoint>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var theOtherTransport = new TestableBridgeTransport(TransportBeingTested);
                theOtherTransport.AddTestEndpoint<MigratedEndpoint>();
                bridgeConfiguration.AddTransport(theOtherTransport);
            })
            .WithEndpoint<MigratedEndpoint>()
            .WithEndpoint<OriginalEndpoint>(endpoint => endpoint.When((session, context) =>
            {
                context.SendStartTime = DateTimeOffset.UtcNow;
                return Task.CompletedTask;
            }))
            .Done(c => c.SendStartTime != DateTimeOffset.MinValue && DateTimeOffset.UtcNow >= c.SendStartTime.AddSeconds(10))
            .Run();

        Assert.That(ctx.NumberOfMessagesReceived, Is.EqualTo(1));
    }

    public class Context : ScenarioContext
    {
        public int NumberOfMessagesReceived { get; set; }
        public DateTimeOffset SendStartTime { get; set; }
    }

    public class MigratedEndpoint : EndpointConfigurationBuilder
    {
        public MigratedEndpoint()
        {
            CustomEndpointName(OriginalEndpointName);
            EndpointSetup<DefaultTestServer>(c =>
            {
                // Set concurrency to 1 to ensure that the first message
                // will delay sufficiently long for the TTBR setting
                // to expire on the second message
                c.LimitMessageProcessingConcurrencyTo(1);
                c.PurgeOnStartup(true);
            });
        }

        public class AMessageHandler(Context testContext) : IHandleMessages<AMessage>
        {
            public async Task Handle(AMessage message, IMessageHandlerContext context)
            {
                testContext.NumberOfMessagesReceived++;

                await Task.Delay(5000);
            }
        }
    }

    public class OriginalEndpoint : EndpointConfigurationBuilder
    {
        public OriginalEndpoint() => EndpointSetup<DefaultServer>(cfg => cfg.EnableFeature<SendLocalFeature>());

        public class SendLocalFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask(() => new SendLocalStartupTask());

            class SendLocalStartupTask : FeatureStartupTask
            {
                protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    await session.SendLocal(new AMessage(), cancellationToken);
                    await session.SendLocal(new AMessage(), cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }
    }

    [TimeToBeReceived("00:00:03")]
    public class AMessage : IMessage;
}