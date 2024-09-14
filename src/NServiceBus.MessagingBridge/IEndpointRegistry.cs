interface IEndpointRegistry
{
    TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName);

    AddressMap AddressMap { get; }

    string GetEndpointAddress(string endpointName);
}
