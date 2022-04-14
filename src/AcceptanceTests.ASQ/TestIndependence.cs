using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
using NServiceBus.MessageInterfaces;
using NServiceBus.Pipeline;
using NServiceBus.Serialization;
using NServiceBus.Settings;

public class TestIndependence
{
    public const string HeaderName = "$AcceptanceTesting.TestRunId";

    public class TestIdAppendingSerializationDefinition<TOriginalSerializationDefintion> : SerializationDefinition
        where TOriginalSerializationDefintion : SerializationDefinition, new()
    {
        public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
        {
            var builder = new TOriginalSerializationDefintion().Configure(settings);
            var scenarioContext = settings.GetOrDefault<ScenarioContext>();

            return mapper => Builder(builder, mapper, scenarioContext);
        }

        static IMessageSerializer Builder(Func<IMessageMapper, IMessageSerializer> builder, IMessageMapper mapper, ScenarioContext scenarioContext)
        {
            var serializer = builder(mapper);
            return new TestIdSerializer(serializer, GetTestRunId(scenarioContext));
        }

        // All messages that have not yet been stamped (for example raw dispatches) will be stamped by the serializer as a last resort.
        class TestIdSerializer : IMessageSerializer
        {
            readonly IMessageSerializer serializer;
            readonly string testId;

            public TestIdSerializer(IMessageSerializer serializer, string testId)
            {
                this.serializer = serializer;
                this.testId = testId;
            }

            public void Serialize(object message, Stream stream)
            {
                var headers = ((MessageWrapper)message).Headers;
                if (!headers.ContainsKey(HeaderName))
                {
                    headers[HeaderName] = testId;
                }
                serializer.Serialize(message, stream);
            }

            public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null) => serializer.Deserialize(body, messageTypes);

            public string ContentType => serializer.ContentType;
        }
    }

    public class SkipBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
    {
        string testRunId;

        public SkipBehavior(ScenarioContext scenarioContext)
        {
            testRunId = GetTestRunId(scenarioContext);
        }

        public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
        {
            if (!context.Message.Headers.TryGetValue(HeaderName, out var runId) || runId != testRunId)
            {
                Console.WriteLine($"Skipping message {context.Message.MessageId} from previous test run");
                return Task.FromResult(0);
            }

            return next(context);
        }
    }

    // All messages that go out with outgoing logical pipelines will be stamped by this behavior.
    public class StampOutgoingBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        string testRunId;

        public StampOutgoingBehavior(ScenarioContext scenarioContext)
        {
            testRunId = GetTestRunId(scenarioContext);
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            context.Headers[HeaderName] = testRunId;
            return next(context);
        }
    }

    static string GetTestRunId(ScenarioContext scenarioContext)
    {
        return scenarioContext.TestRunId.ToString();
    }
}