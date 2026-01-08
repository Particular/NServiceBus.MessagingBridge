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
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                transportBeingTested.AddTestEndpoint<PublishingEndpoint>();
                transportBeingTested.AddTestEndpoint<AuditSpy>();
                bridgeConfiguration.AddTransport(transportBeingTested);

                var subscriberEndpoint =
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));
                subscriberEndpoint.RegisterPublisher<MessageToBeAudited>(
                    Conventions.EndpointNamingConvention(typeof(PublishingEndpoint)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            }, metadata => metadata.RegisterPublisherFor<MessageToBeAudited, PublishingEndpoint>())
            .WithEndpoint<PublishingEndpoint>(b => b
                .When(c => c.TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, _) => session.Publish(new MessageToBeAudited())))
            .WithEndpoint<ProcessingEndpoint>()
            .WithEndpoint<AuditSpy>()
            .Run();

        foreach (var header in context.AuditMessageHeaders)
        {
            if (context.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
            {
                Assert.That(receivedHeaderValue, Is.EqualTo(header.Value),
                    $"{header.Key} is not the same on processed message and audit message.");
            }
        }
    }

    public class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() =>
            EndpointSetup<DefaultServer>(b =>
            {
                b.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
                b.ConfigureRouting().RouteToEndpoint(typeof(MessageToBeAudited), typeof(ProcessingEndpoint));
            }, metadata => metadata.RegisterSelfAsPublisherFor<MessageToBeAudited>(this));
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultTestServer>(
            b => b.AuditProcessedMessagesTo("Audit.AuditSpy"), metadata => metadata.RegisterPublisherFor<MessageToBeAudited, PublishingEndpoint>());

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
                testContext.AuditMessageHeaders = new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                testContext.MarkAsCompleted();

                return Task.CompletedTask;
            }
        }
    }

    public class Context : BridgeScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> AuditMessageHeaders { get; set; }
    }

    public class MessageToBeAudited : IEvent;
}