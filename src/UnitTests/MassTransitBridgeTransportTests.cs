using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NUnit.Framework;
using UnitTests;

public class MassTransitBridgeTransportTests
{
    [Test]
    public void Should_automatically_apply_adapter_to_manually_registered_endpoints()
    {
        // Arrange
        var transport = new MassTransitBridgeTransport(new FakeTransport());

        // Act
        transport.HasEndpoint("masstransit-queue-1");
        transport.HasEndpoint("masstransit-queue-2", "error");

        // Assert
        Assert.That(transport.Endpoints.Count, Is.EqualTo(2));
        Assert.That(transport.Endpoints[0].MessageFormatAdapter, Is.Not.Null);
        Assert.That(transport.Endpoints[0].MessageFormatAdapter.Name, Is.EqualTo("MassTransit"));
        Assert.That(transport.Endpoints[1].MessageFormatAdapter, Is.Not.Null);
        Assert.That(transport.Endpoints[1].MessageFormatAdapter.Name, Is.EqualTo("MassTransit"));
    }

    [Test]
    public async Task Should_automatically_apply_adapter_to_discovered_endpoints()
    {
        // Arrange
        var transport = new MassTransitBridgeTransport(new FakeTransport());
        var queueDiscovery = new FakeQueueDiscovery(["queue1", "queue2", "queue3"]);

        // Act
        await transport.DiscoverQueues(queueDiscovery);

        // Assert
        Assert.That(transport.Endpoints.Count, Is.EqualTo(3));
        Assert.That(transport.Endpoints.All(e => e.MessageFormatAdapter != null), Is.True);
        Assert.That(transport.Endpoints.All(e => e.MessageFormatAdapter.Name == "MassTransit"), Is.True);
    }

    [Test]
    public void Should_support_fluent_chaining()
    {
        // Arrange
        var transport = new MassTransitBridgeTransport(new FakeTransport());

        // Act
        transport
            .HasEndpoint("queue1")
            .HasEndpoint("queue2")
            .HasEndpoint("queue3", "error");

        // Assert
        Assert.That(transport.Endpoints.Count, Is.EqualTo(3));
    }

    [Test]
    public void Should_work_with_bridge_configuration()
    {
        // Arrange
        var bridge = new BridgeConfiguration();
        var nsbTransport = new BridgeTransport(new FakeTransport());
        var mtTransport = new MassTransitBridgeTransport(new FakeTransport());

        // Differentiate transports by name
        nsbTransport.Name = "nsb";
        mtTransport.Name = "mt";

        // Act
        bridge.AddTransport(nsbTransport);
        nsbTransport.HasEndpoint("ServiceControl");

        bridge.AddTransport(mtTransport);
        mtTransport.HasEndpoint("masstransit-error");

        // Assert - should not throw during configuration
        Assert.That(nsbTransport.Endpoints.Count, Is.EqualTo(1));
        Assert.That(mtTransport.Endpoints.Count, Is.EqualTo(1));
        Assert.That(mtTransport.Endpoints[0].MessageFormatAdapter, Is.Not.Null);
    }

    [Test]
    public async Task Should_handle_file_based_queue_discovery()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, new[] { "queue1", "queue2", "queue3" });

        try
        {
            var transport = new MassTransitBridgeTransport(new FakeTransport());
            var queueDiscovery = new FileBasedQueueDiscovery(tempFile);

            // Act
            await transport.DiscoverQueues(queueDiscovery);

            // Assert
            Assert.That(transport.Endpoints.Count, Is.EqualTo(3));
            Assert.That(transport.Endpoints.All(e => e.MessageFormatAdapter != null), Is.True);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    class FakeQueueDiscovery : IQueueDiscovery
    {
        readonly string[] queues;

        public FakeQueueDiscovery(string[] queues)
        {
            this.queues = queues;
        }

        public async IAsyncEnumerable<string> GetQueues([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var queue in queues)
            {
                yield return queue;
            }
            await Task.CompletedTask;
        }
    }
}
