interface IEndpointRegistry
{
    TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName);

    bool TryTranslateToTargetAddress(string sourceAddress, out string bestMatch);

    string GetEndpointAddress(string endpointName);
}
