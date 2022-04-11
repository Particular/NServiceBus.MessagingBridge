using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.AcceptanceTests;
using NServiceBus.Transport;

public class TestableSQSTransport : SqsTransport
{
    public TestableSQSTransport(string namePrefix) : base(CreateSqsClient(), CreateSnsClient())
    {
        QueueNamePrefix = namePrefix;
        TopicNamePrefix = namePrefix;
        QueueNameGenerator = TestNameHelper.GetSqsQueueName;
        TopicNameGenerator = TestNameHelper.GetSnsTopicName;

        S3BucketName = Environment.GetEnvironmentVariable(S3BucketEnvironmentVariableName);

        if (!string.IsNullOrEmpty(S3BucketName))
        {
            S3 = new S3Settings(S3BucketName, S3Prefix, CreateS3Client());
        }
    }

    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings,
        ReceiveSettings[] receivers,
        string[] sendingAddresses,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var infrastructure = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken)
            .ConfigureAwait(false);
        QueuesToCleanup.AddRange(infrastructure.Receivers.Select(x => x.Value.ReceiveAddress).Concat(sendingAddresses)
            .Distinct());
        return infrastructure;
    }

    public static IAmazonSQS CreateSqsClient()
    {
        var credentials = new EnvironmentVariablesAWSCredentials();
        return new AmazonSQSClient(credentials);
    }

    public static IAmazonSimpleNotificationService CreateSnsClient()
    {
        var credentials = new EnvironmentVariablesAWSCredentials();
        return new AmazonSimpleNotificationServiceClient(credentials);
    }

    public static IAmazonS3 CreateS3Client()
    {
        var credentials = new EnvironmentVariablesAWSCredentials();
        return new AmazonS3Client(credentials);
    }

    string S3BucketEnvironmentVariableName = "NSERVICEBUS_AMAZONSQS_S3BUCKET";
    string S3Prefix = "test";
    string S3BucketName;
    public List<string> QueuesToCleanup { get; } = new List<string>();
}