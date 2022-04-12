using System;
//using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureSQSTransportTestExecution : IConfigureTransportTestExecution
{
    //string NamePrefix = $"AT{Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "").ToUpperInvariant()}";

    public BridgeTransportDefinition GetBridgeTransport()
    {
        var transportDefinition = new TestableSQSTransport(NamePrefixGenerator.GetNamePrefix());

        return new BridgeTransportDefinition()
        {
            TransportDefinition = transportDefinition,
            Cleanup = (ct) => Cleanup(ct),
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableSQSTransport(NamePrefixGenerator.GetNamePrefix());
        endpointConfiguration.UseTransport(transportDefinition);

        return ct => Cleanup(ct);
    }

    async Task Cleanup(CancellationToken cancellationToken)
    {
        var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        using (var sqsClient = new AmazonSQSClient(accessKeyId, secretAccessKey))
        using (var snsClient = new AmazonSimpleNotificationServiceClient(accessKeyId, secretAccessKey))
        using (var s3Client = new AmazonS3Client(accessKeyId, secretAccessKey))
        {
            await SQSCleanup.DeleteAllResourcesWithPrefix(sqsClient, snsClient, s3Client, NamePrefixGenerator.GetNamePrefix()).ConfigureAwait(false);
        }

    }
}