using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class DefaultTestPublisher : IEndpointSetupTemplate
{
#pragma warning disable PS0013 // Add a CancellationToken parameter type argument
    public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
#pragma warning restore PS0013 // Add a CancellationToken parameter type argument
    {
        return new DefaultTestServer().GetConfiguration(runDescriptor, endpointConfiguration, configurationBuilderCustomization);
    }
}

