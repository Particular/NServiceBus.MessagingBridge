using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class Error : BridgeAcceptanceTest
{
    [Test]
    public async Task Should_forward_error_messages_and_not_modify_header_other_than_ReplyToAddress()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge((bridgeConfiguration, transportBeingTested) =>
            {
                transportBeingTested.AddTestEndpoint<PublishingEndpoint>();
                transportBeingTested.AddTestEndpoint<ErrorSpy>();
                bridgeConfiguration.AddTransport(transportBeingTested);

                var subscriberEndpoint =
                    new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint)));
                subscriberEndpoint.RegisterPublisher<FaultyMessage>(Conventions.EndpointNamingConvention(typeof(PublishingEndpoint)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);

            }, metadata => metadata.RegisterPublisherFor<FaultyMessage, PublishingEndpoint>())
            .WithEndpoint<PublishingEndpoint>(b => b
                .When(c => c.TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, _) => session.Publish(new FaultyMessage())))
            .WithEndpoint<ProcessingEndpoint>(builder => builder.DoNotFailOnErrorMessages())
            .WithEndpoint<ErrorSpy>()
            .Done(c => c.MessageFailed)
            .Run();

        Assert.That(context.MessageFailed, Is.True);
        foreach (var header in context.FailedMessageHeaders)
        {
            if (header.Key != Headers.ReplyToAddress)
            {
                if (context.ReceivedMessageHeaders.TryGetValue(header.Key, out var receivedHeaderValue))
                {
                    Assert.That(receivedHeaderValue, Is.EqualTo(header.Value),
                        $"{header.Key} is not the same on processed message and error message.");
                }
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
                c.ConfigureRouting().RouteToEndpoint(typeof(FaultyMessage), typeof(ProcessingEndpoint));
            }, metadata => metadata.RegisterSelfAsPublisherFor<FaultyMessage>(this));
    }

    public class ProcessingEndpoint : EndpointConfigurationBuilder
    {
        public ProcessingEndpoint() => EndpointSetup<DefaultTestServer>(
            c => c.SendFailedMessagesTo("Error.ErrorSpy"), metadata => metadata.RegisterPublisherFor<FaultyMessage, PublishingEndpoint>());

        public class MessageHandler(Context testContext) : IHandleMessages<FaultyMessage>
        {
            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                throw new Exception("Simulated");
            }
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(c => c.AutoSubscribe().DisableFor<FaultyMessage>());

        class FailedMessageHander(Context testContext) : IHandleMessages<FaultyMessage>
        {
            public Task Handle(FaultyMessage message, IMessageHandlerContext context)
            {
                testContext.FailedMessageHeaders =
                    new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);

                testContext.MessageFailed = true;

                return Task.CompletedTask;
            }
        }
    }

    public class Context : BridgeScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool MessageFailed { get; set; }
        public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        public IReadOnlyDictionary<string, string> FailedMessageHeaders { get; set; }
    }

    public class FaultyMessage : IEvent;
}