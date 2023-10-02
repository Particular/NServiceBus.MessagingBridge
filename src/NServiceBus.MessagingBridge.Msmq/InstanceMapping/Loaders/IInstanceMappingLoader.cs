namespace NServiceBus.MessagingBridge.Msmq
{
    using System.Xml.Linq;

    interface IInstanceMappingLoader
    {
        XDocument Load();
    }
}