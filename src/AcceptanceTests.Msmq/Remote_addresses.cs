﻿using System.Threading.Tasks;
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
                        .When(cc => cc.EndpointsStarted, (b, _) => b.SendLocal(new StartMessage())))
                    .WithEndpoint<ReplyingEndpoint>()
                    .WithEndpoint<ReplyReceivingEndpoint>()
                    .WithBridge(bridgeConfiguration =>
                    {
                        var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                        bridgeTransport.AddTestEndpoint<SendingEndpoint>();

                        var replyReceivingEndpointName =
                            Conventions.EndpointNamingConvention(typeof(ReplyReceivingEndpoint));
                        var replyReceivingEndpoint = new BridgeEndpoint(replyReceivingEndpointName, replyReceivingEndpointName + "@someserver");

                        bridgeTransport.HasEndpoint(replyReceivingEndpoint);

                        bridgeConfiguration.AddTransport(bridgeTransport);

                        bridgeConfiguration.AddTestTransportEndpoint<ReplyingEndpoint>();
                    })
                    .Done(c => c.SendingEndpointGotResponse)
                    .Run();

        Assert.That(ctx.SendingEndpointGotResponse, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool SendingEndpointGotResponse { get; set; }
    }

    public class SendingEndpoint : EndpointConfigurationBuilder
    {
        public SendingEndpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(ReplyingEndpoint));
            });
        }

        public class ResponseHandler : IHandleMessages<StartMessage>
        {
            readonly ITransportAddressResolver transportAddressResolver;

            public ResponseHandler(ITransportAddressResolver transportAddressResolver)
            {
                this.transportAddressResolver = transportAddressResolver;
            }

            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                var sendOptions = new SendOptions();
                var endpointName = NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ReplyReceivingEndpoint));
                var endpointAddress = transportAddressResolver.ToTransportAddress(new QueueAddress(endpointName));
                sendOptions.RouteReplyTo(endpointAddress);

                return context.Send(new MyMessage(), sendOptions);
            }
        }
    }

    public class ReplyReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReplyReceivingEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class ResponseHandler : IHandleMessages<MyReply>
        {
            public ResponseHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyReply messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.SendingEndpointGotResponse = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class ReplyingEndpoint : EndpointConfigurationBuilder
    {
        public ReplyingEndpoint()
        {
            EndpointSetup<DefaultTestServer>();
        }

        public class MessageHandler : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return context.Reply(new MyReply());
            }
        }
    }

    public class StartMessage : IMessage
    {
    }

    public class MyMessage : IMessage
    {
    }

    public class MyReply : IMessage
    {
    }
}