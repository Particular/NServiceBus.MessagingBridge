interface IEndpointRegistry
{
    TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName);

    bool TryTranslateToTargetAddress(string sourceAddress, out string targetAddress);

    string TranslateToTargetAddress(string sourceAddress);

    string GetEndpointAddress(string endpointName);
}
