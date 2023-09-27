using System;
//using System.Collections.Generic;
//using System.Linq;
//using MSMQ.Messaging;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
//using NServiceBus.Transport;

class ConfigureMsmqTransportTestExecution : IConfigureTransportTestExecution
{
    public BridgeTransportDefinition GetBridgeTransport()
    {
        //var transportDefinition = new TestableMsmqTransport();

        return new BridgeTransportDefinition
        {
            TransportDefinition = null,//transportDefinition,
            //Cleanup = (ct) => Cleanup(transportDefinition, ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        //var transportDefinition = new TestableMsmqTransport();
        //var routingConfig = endpointConfiguration.UseTransport(transportDefinition);
        //endpointConfiguration.UsePersistence<MsmqPersistence, StorageType.Subscriptions>();

        //foreach (var publisher in publisherMetadata.Publishers)
        //{
        //    foreach (var eventType in publisher.Events)
        //    {
        //        routingConfig.RegisterPublisher(eventType, publisher.PublisherName);
        //    }
        //}

        //return (ct) => Cleanup(transportDefinition, ct);
        return (_) => Task.CompletedTask;
    }

    //static Task Cleanup(TestableMsmqTransport msmqTransport, CancellationToken cancellationToken)
    //{
    //    var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
    //    var queuesToBeDeleted = new List<string>();

    //    foreach (var messageQueue in allQueues)
    //    {
    //        using (messageQueue)
    //        {
    //            if (msmqTransport.ReceiveQueues.Any(ra =>
    //            {
    //                var indexOfAt = ra.IndexOf("@", StringComparison.Ordinal);
    //                if (indexOfAt >= 0)
    //                {
    //                    ra = ra.Substring(0, indexOfAt);
    //                }
    //                return messageQueue.QueueName.StartsWith(@"private$\" + ra, StringComparison.OrdinalIgnoreCase);
    //            }))
    //            {
    //                queuesToBeDeleted.Add(messageQueue.Path);
    //            }
    //        }
    //    }

    //    foreach (var queuePath in queuesToBeDeleted)
    //    {
    //        try
    //        {
    //            MessageQueue.Delete(queuePath);
    //            Console.WriteLine("Deleted '{0}' queue", queuePath);
    //        }
    //        catch (Exception)
    //        {
    //            Console.WriteLine("Could not delete queue '{0}'", queuePath);
    //        }
    //    }

    //    MessageQueue.ClearConnectionCache();

    //    return Task.CompletedTask;
    //}
}