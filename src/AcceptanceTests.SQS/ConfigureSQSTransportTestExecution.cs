﻿using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureSQSTransportTestExecution : IConfigureTransportTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableSQSTransport(NamePrefixGenerator.GetNamePrefix());
        endpointConfiguration.UseTransport(transportDefinition);
        return Task.CompletedTask;
    }

    // Don't need to return the cleanup function here; all the queues will be cleaned up in the
    // bridge transport cleanup call and doubling it up here leads to delays as SQS tries to
    // delete the queues twice in rapid succession
    public Task Cleanup() => Task.CompletedTask;

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new TestableSQSTransport(NamePrefixGenerator.GetNamePrefix()).ToTestableBridge();

    public async Task Cleanup(BridgeTransport bridgeTransport)
    {
        var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        using var sqsClient = new AmazonSQSClient(accessKeyId, secretAccessKey);
        using var snsClient = new AmazonSimpleNotificationServiceClient(accessKeyId, secretAccessKey);
        using var s3Client = new AmazonS3Client(accessKeyId, secretAccessKey);
        await SQSCleanup.DeleteAllResourcesWithPrefix(sqsClient, snsClient, s3Client, NamePrefixGenerator.GetNamePrefix()).ConfigureAwait(false);
    }
}