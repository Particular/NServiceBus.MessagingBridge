using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBM.WMQ;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport.IBMMQ;

class ConfigureIBMMQTransportTestExecution : IConfigureTransportTestExecution
{
    readonly List<string> queuesToCleanup = [];

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        var naming = TestConnectionDetails.CreateTopicNaming();

        var transport = new IBMMQTransport
        {
            MessageWaitInterval = TimeSpan.FromMilliseconds(100),
            TopicNaming = naming,
            ResourceNameSanitizer = Sanitize
        };
        TestConnectionDetails.Apply(transport);

        foreach (var eventType in publisherMetadata.Publishers.SelectMany(p => p.Events))
        {
            var topicString = naming.GenerateTopicString(eventType);
            transport.Topology.SubscribeTo(eventType, topicString);
            transport.Topology.PublishTo(eventType, topicString);
        }

        endpointConfiguration.UseTransport(transport);
        queuesToCleanup.Add(Sanitize(endpointName));
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        PurgeQueues(queuesToCleanup);
        return Task.CompletedTask;
    }

    public BridgeTransport Configure(PublisherMetadata publisherMetadata)
    {
        var naming = TestConnectionDetails.CreateTopicNaming();

        var transport = new IBMMQTransport
        {
            MessageWaitInterval = TimeSpan.FromMilliseconds(100),
            TopicNaming = naming,
            ResourceNameSanitizer = Sanitize
        };
        TestConnectionDetails.Apply(transport);

        foreach (var eventType in publisherMetadata.Publishers.SelectMany(p => p.Events))
        {
            var topicString = naming.GenerateTopicString(eventType);
            transport.Topology.SubscribeTo(eventType, topicString);
            transport.Topology.PublishTo(eventType, topicString);
        }

        return transport.ToTestableBridge();
    }

    public Task Cleanup(BridgeTransport bridgeTransport)
    {
        // Bridge queues are cleaned up by the bridge infrastructure
        return Task.CompletedTask;
    }

    static string Sanitize(string name)
    {
        name = name.Replace('-', '.');

        if (name.Length <= 48)
        {
            return name;
        }

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var hashHex = Convert.ToHexString(XxHash32.Hash(nameBytes));

        int prefixLength = 48 - hashHex.Length;
        var prefix = name[..Math.Min(prefixLength, name.Length)];
        return $"{prefix}{hashHex}";
    }

    static void PurgeQueues(List<string> queues)
    {
        if (queues.Count == 0)
        {
            return;
        }

        try
        {
            var connectionProperties = new Hashtable
            {
                { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
                { MQC.HOST_NAME_PROPERTY, TestConnectionDetails.Host },
                { MQC.PORT_PROPERTY, TestConnectionDetails.Port },
                { MQC.CHANNEL_PROPERTY, TestConnectionDetails.Channel },
                { MQC.USER_ID_PROPERTY, TestConnectionDetails.User },
                { MQC.PASSWORD_PROPERTY, TestConnectionDetails.Password }
            };

            using var queueManager = new MQQueueManager(TestConnectionDetails.QueueManagerName, connectionProperties);

            foreach (var queueName in queues.Distinct())
            {
                try
                {
                    using var queue = queueManager.AccessQueue(queueName, MQC.MQOO_INPUT_AS_Q_DEF);
                    var gmo = new MQGetMessageOptions { Options = MQC.MQGMO_NO_WAIT | MQC.MQGMO_ACCEPT_TRUNCATED_MSG };

                    while (true)
                    {
                        try
                        {
                            var message = new MQMessage();
                            queue.Get(message, gmo);
                            message.ClearMessage();
                        }
                        catch (MQException ex) when (ex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                        {
                            break;
                        }
                    }

                    queue.Close();
                }
                catch (MQException)
                {
                    // Queue may not exist, ignore
                }
            }

            queueManager.Disconnect();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to clean up IBM MQ queues: {0}", ex);
        }
    }
}