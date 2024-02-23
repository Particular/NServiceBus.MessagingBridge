namespace NServiceBus
{
    using System;

    class HeartbeatConfiguration
    {
        public string ServiceControlQueue { get; set; }
        public TimeSpan Frequency { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan TimeToLive { get; set; }
    }
}