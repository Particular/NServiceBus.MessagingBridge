using System;
using System.Security.Cryptography;
using System.Text;
using NServiceBus.Transport.IBMMQ;

class ShortenedTopicNaming : TopicNaming
{
    readonly string prefix;

    public ShortenedTopicNaming(string topicPrefix = "DEV") : base(topicPrefix) => prefix = topicPrefix;

    public override string GenerateTopicName(Type eventType)
    {
        var fullName = (eventType.FullName ?? eventType.Name).Replace('+', '.').ToUpperInvariant();
        var name = $"{prefix.ToUpperInvariant()}.{fullName}";

        if (name.Length <= 48)
        {
            return name;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8];
        return $"{name[..(48 - 9)]}_{hash}";
    }
}