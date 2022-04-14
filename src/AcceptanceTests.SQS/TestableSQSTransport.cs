using System;
using Amazon.Runtime;
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

    public static IAmazonSQS CreateSqsClient()
    {
        return new AmazonSQSClient();
    }

    public static IAmazonSimpleNotificationService CreateSnsClient()
    {
        return new AmazonSimpleNotificationServiceClient();
    }

    public static IAmazonS3 CreateS3Client()
    {
        return new AmazonS3Client();
    }

    string S3BucketEnvironmentVariableName = "NSERVICEBUS_AMAZONSQS_S3BUCKET";
    string S3Prefix = "test";
    string S3BucketName;
}