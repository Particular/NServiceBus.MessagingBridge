using System;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.AcceptanceTests;

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

    // Fixtures run in parallel against one account and hit control-plane throttling with the default retries.
    const int MaxErrorRetry = 10;

    public static IAmazonSQS CreateSqsClient()
    {
        return new AmazonSQSClient(new AmazonSQSConfig { MaxErrorRetry = MaxErrorRetry });
    }

    public static IAmazonSimpleNotificationService CreateSnsClient()
    {
        return new AmazonSimpleNotificationServiceClient(new AmazonSimpleNotificationServiceConfig { MaxErrorRetry = MaxErrorRetry });
    }

    public static IAmazonS3 CreateS3Client()
    {
        return new AmazonS3Client();
    }

    string S3BucketEnvironmentVariableName = "NSERVICEBUS_AMAZONSQS_S3BUCKET";
    string S3Prefix = "test";
    string S3BucketName;
}