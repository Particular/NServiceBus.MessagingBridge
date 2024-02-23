namespace NServiceBus;

using System;

class CustomChecksConfiguration
{
    public string ServiceControlQueue { get; set; }

    public TimeSpan? TimeToLive { get; set; }

    public Type[] CustomCheckTypes { get; set; }
}