interface IEndpointRegistry
{
    TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName);

    IAddressMap AddressMap { get; }

    string GetEndpointAddress(string endpointName);
}
