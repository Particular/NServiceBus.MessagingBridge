﻿using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Publishing : RouterAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var routerConfiguration = new MessageRouterConfiguration();

        routerConfiguration.AddTransport(TransportBeingTested)
            .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Publisher)));

        routerConfiguration.AddTransport(DefaultTestServer.GetTestTransportDefinition())
            .HasEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)))
             .RegisterPublisher(typeof(MyEvent).FullName, Conventions.EndpointNamingConvention(typeof(Publisher)));


        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b
                .When(c => c.HasNativePubSubSupport || c.SubscriberSubscribed, (session, c) =>
                {
                    var options = new PublishOptions();

                    return session.Publish(new MyEvent(), options);
                }))
            .WithEndpoint<Subscriber>()
            .WithRouter(routerConfiguration)
            .Done(c => c.SubscriberGotEvent)
            .Run().ConfigureAwait(false);

        Assert.IsTrue(context.SubscriberGotEvent);
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool SubscriberGotEvent { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultTestPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
            });
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<AutoSubscribe>();
            }, p => p.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
        }

        public class MessageHandler : IHandleMessages<MyEvent>
        {
            Context context;

            public MessageHandler(Context context) => this.context = context;

            public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
            {
                context.SubscriberGotEvent = true;
                return Task.FromResult(0);
            }
        }
    }

    public class MyEvent : IEvent
    {
    }
}