using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Send_local : BridgeAcceptanceTest
{
    static string OriginalEndpointName = Conventions.EndpointNamingConvention(typeof(OriginalEndpoint));

    [Test]
    public async Task Should_transfer_send_local_message()
    {
        var ctx = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                transportBeingTested.HasEndpoint("_");
                bridgeConfiguration.AddTransport(transportBeingTested);

                bridgeConfiguration.AddTestTransportEndpoint(new BridgeEndpoint(OriginalEndpointName));
            })
            .WithEndpoint<OriginalEndpoint>()
            .WithEndpoint<MigratedEndpoint>()
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(ctx.MessageReceived, Is.True);
    }

    public class Context : BridgeScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    public class MigratedEndpoint : EndpointConfigurationBuilder
    {
        public MigratedEndpoint()
        {
            CustomEndpointName(OriginalEndpointName);
            EndpointSetup<DefaultTestServer>();
        }

        public class AMessageHandler(Context testContext) : IHandleMessages<AMessage>
        {
            public Task Handle(AMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                return Task.CompletedTask;
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

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }
    }

    public class AMessage : IMessage;
}