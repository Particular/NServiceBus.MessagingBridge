using System;
using System.Collections.Specialized;
using System.Web;
using NServiceBus.Transport.IBMMQ;

static class TestConnectionDetails
{
    static readonly Uri ConnectionUri = new(Environment.GetEnvironmentVariable("IBMMQ_CONNECTIONSTRING") ?? "mq://admin:passw0rd@localhost:1414");
    static readonly NameValueCollection Query = HttpUtility.ParseQueryString(ConnectionUri.Query);

    public static string Host => ConnectionUri.Host;
    public static int Port => ConnectionUri.Port > 0 ? ConnectionUri.Port : 1414;
    public static string User => Uri.UnescapeDataString(ConnectionUri.UserInfo.Split(':')[0]);
    public static string Password => Uri.UnescapeDataString(ConnectionUri.UserInfo.Split(':')[1]);
    public static string QueueManagerName => ConnectionUri.AbsolutePath.Trim('/') is { Length: > 0 } path ? Uri.UnescapeDataString(path) : "QM1";
    public static string Channel => Query["channel"] ?? "DEV.ADMIN.SVRCONN";
    public static string TopicPrefix => Query["topicprefix"] ?? "DEV";

    public static TopicNaming CreateTopicNaming() => new ShortenedTopicNaming(TopicPrefix);

    public static void Apply(IBMMQTransport transport)
    {
        transport.Host = Host;
        transport.Port = Port;
        transport.User = User;
        transport.Password = Password;
        transport.QueueManagerName = QueueManagerName;
        transport.Channel = Channel;

        if (Query["appname"] is { } appName)
        {
            transport.ApplicationName = appName;
        }
        if (Query["sslkeyrepo"] is { } sslKeyRepo)
        {
            transport.SslKeyRepository = sslKeyRepo;
        }
        if (Query["cipherspec"] is { } cipherSpec)
        {
            transport.CipherSpec = cipherSpec;
        }
        if (Query["sslpeername"] is { } sslPeerName)
        {
            transport.SslPeerName = sslPeerName;
        }
    }
}