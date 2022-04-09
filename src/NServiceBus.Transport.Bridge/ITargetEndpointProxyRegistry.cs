using NServiceBus.Raw;

interface ITargetEndpointProxyRegistry
{
    IRawEndpoint GetTargetEndpointProxy(string sourceEndpointName);
}