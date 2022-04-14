namespace Testing
{
    using System;
    using NServiceBus;

    public static class Utilities
    {
        public static string GetEnvConfiguredConnectionString()
        {
            var environmentVariableName = $"{nameof(AzureStorageQueueTransport)}_ConnectionString";
            var connectionString = GetEnvironmentVariable(environmentVariableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Couldn't find an environment variable '{environmentVariableName}' with Azure Storage connection string.");
            }

            return connectionString;
        }

        public static string GetEnvConfiguredConnectionString2()
        {
            var environmentVariableName = $"{nameof(AzureStorageQueueTransport)}_ConnectionString_2";
            var connectionString = GetEnvironmentVariable(environmentVariableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Couldn't find an environment variable '{environmentVariableName}' with Azure Storage connection string.");
            }

            return connectionString;
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }

        public static TestableAzureStorageQueuesTransport CreateTransportWithDefaultTestsConfiguration(string connectionString, string delayedDeliveryPoisonQueue = null)
        {
            var transport = new TestableAzureStorageQueuesTransport(connectionString);
            return SetTransportDefaultTestsConfiguration(transport, delayedDeliveryPoisonQueue);
        }

        public static TestableAzureStorageQueuesTransport SetTransportDefaultTestsConfiguration(TestableAzureStorageQueuesTransport transport, string delayedDeliveryPoisonQueue = null)
        {
            transport.MessageInvisibleTime = TimeSpan.FromSeconds(30);
            transport.MessageWrapperSerializationDefinition = new TestIndependence.TestIdAppendingSerializationDefinition<NewtonsoftSerializer>();
            transport.QueueNameSanitizer = BackwardsCompatibleQueueNameSanitizerForTests.Sanitize;

            if (delayedDeliveryPoisonQueue != null)
            {
                transport.DelayedDelivery.DelayedDeliveryPoisonQueue = delayedDeliveryPoisonQueue;
            }

            return transport;
        }
    }
}
