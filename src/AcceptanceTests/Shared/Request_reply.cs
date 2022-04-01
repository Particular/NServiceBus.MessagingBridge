using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.Support;
using NServiceBus.Transport;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Request_reply
{
    [Test]
    public async Task Should_get_the_reply()
    {
        var routerConfiguration = new MessageRouterConfiguration();

        var addressOfSendingEndpoint = new QueueAddress(Conventions.EndpointNamingConvention(typeof(SendingEndpoint)),
            properties: new Dictionary<string, string> { { "machine", RuntimeEnvironment.MachineName } });

        routerConfiguration.AddTransport(new MsmqTransport())
          .HasEndpoint(addressOfSendingEndpoint);

        routerConfiguration.AddTransport(new LearningTransport())
            .HasEndpoint(Conventions.EndpointNamingConvention(typeof(ReplyingEndpoint)));

        var ctx = await Scenario.Define<Context>()
                    .WithEndpoint<SendingEndpoint>(c => c
                        .When(b => b.Send(new MyMessage())))
                    .WithEndpoint<ReplyingEndpoint>()
                    .WithRouter(routerConfiguration)
                    .Done(c => c.SendingEndpointGotResponse)
                    .Run().ConfigureAwait(false);

        Assert.IsTrue(ctx.SendingEndpointGotResponse);
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
                c.UseTransport(new MsmqTransport());
                c.UsePersistence<MsmqPersistence, StorageType.Subscriptions>();
                c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(ReplyingEndpoint));
            });
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
            EndpointSetup<DefaultServer>(c =>
            {
                c.UseTransport(new LearningTransport());
            });
        }

        public class MessageHandler : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return context.Reply(new MyReply());
            }
        }
    }

    public class MyMessage : IMessage
    {
    }

    public class MyReply : IMessage
    {
    }
}