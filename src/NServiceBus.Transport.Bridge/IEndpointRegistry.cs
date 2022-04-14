interface IEndpointRegistry
{
    TargetEndpointDispatcher GetTargetEndpointDispatcher(string sourceEndpointName);

    string TranslateToTargetAddress(string sourceAddress);
}
