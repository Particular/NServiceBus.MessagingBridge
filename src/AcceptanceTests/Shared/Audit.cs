using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Audit : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_forward_audit_messages_by_not_modify_message()
    {
        var ctx = await Scenario.Define<Context>()
            .WithEndpoint<PublishingEndpoint>(b => b
                .When(c => TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, _) => session.Publish(new MessageToBeAudited())))
            .WithEndpoint<ProcessingEndpoint>()
            .WithEndpoint<AuditSpy>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);
                bridgeTransport.AddTestEndpoint<PublishingEndpoint>();
                bridgeTransport.AddTestEndpoint<AuditSpy>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint =
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));
                subscriberEndpoint.RegisterPublisher<MessageToBeAudited>(
                    Conventions.EndpointNamingConvention(typeof(PublishingEndpoint)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            })
            .Done(c => c.MessageAudited)
            .Run();

        Assert.That(ctx.MessageAudited, Is.True);
        foreach (var header in ctx.AuditMessageHeaders)
        {
            if (ctx.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
            {
                Assert.That(receivedHeaderValue, Is.EqualTo(header.Value),
                    $"{header.Key} is not the same on processed message and audit message.");
            }
        }
    }

    public class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
                c.ConfigureRouting().RouteToEndpoint(typeof(MessageToBeAudited), typeof(ProcessingEndpoint));
            }, metadata => metadata.RegisterSelfAsPublisherFor<MessageToBeAudited>(this));
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultTestServer>(
            c => c.AuditProcessedMessagesTo("Audit.AuditSpy"), metadata => metadata.RegisterPublisherFor<MessageToBeAudited, PublishingEndpoint>());

        public class MessageHandler(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                return Task.CompletedTask;
            }
        }
    }

    public class AuditSpy : EndpointConfigurationBuilder
    {
        public AuditSpy() => EndpointSetup<DefaultServer>(c => c.AutoSubscribe().DisableFor<MessageToBeAudited>());

        class AuditMessageHander(Context testContext) : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
            {
                testContext.MessageAudited = true;
                testContext.AuditMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                return Task.CompletedTask;
            }
        }
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool MessageAudited { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> AuditMessageHeaders { get; set; }
    }

    public class MessageToBeAudited : IEvent;
}