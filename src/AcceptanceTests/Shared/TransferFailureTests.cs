using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AcceptanceTesting;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class TransferFailureTests : BridgeAcceptanceTest
{
    const string ReceiveDummyQueue = "TransferFailureTests_DummyQueue"; // Required because Bridge needs endpoints on both sides.
    const string ErrorQueue = "TransferFailureTests_BridgeErrorQueue";
    const string FailedQHeader = "NServiceBus.MessagingBridge.FailedQ";

    [Test]
    public async Task Should_add_failedq_header_when_transfer_fails()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<ErrorSpy>()
            .WithEndpoint<Sender>(b => b
                .When(c => c.EndpointsStarted, async (session, c) =>
                {
                    var opts = new SendOptions();
                    opts.SetHeader(FakeShovelHeader.FailureHeader, string.Empty);
                    await session.Send(new FaultyMessage(), opts);
                }))
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);
                bridgeTransport.AddTestEndpoint<Sender>();
                bridgeConfiguration.AddTransport(bridgeTransport);
                bridgeTransport.ErrorQueue = ErrorQueue;

                var subscriberEndpoint = new BridgeEndpoint(ReceiveDummyQueue);
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            })
            .Done(c => c.MessageFailed)
            .Run();

        Assert.IsTrue(ctx.MessageFailed, "Message did not fail");
        Assert.IsTrue(ctx.FailedMessageHeaders.ContainsKey(FailedQHeader),
            $"Failed message headers does not contain {FailedQHeader}");
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(FaultyMessage), ReceiveDummyQueue);
            });
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.OverrideLocalAddress(ErrorQueue);
            });
        }

        class FailedMessageHander : IHandleMessages<FaultyMessage>
        {
            public FailedMessageHander(Context context) => testContext = context;

            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.FailedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                testContext.MessageFailed = true;
                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
    }

    public class FaultyMessage : ICommand
    {
    }
}