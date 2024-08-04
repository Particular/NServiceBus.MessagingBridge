interface IEndpointRegistry
{
    TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName);

    bool TryTranslateToTargetAddress(string sourceAddress, out (string targetAddress, string nearestMatch) result);

    string GetEndpointAddress(string endpointName);
}
