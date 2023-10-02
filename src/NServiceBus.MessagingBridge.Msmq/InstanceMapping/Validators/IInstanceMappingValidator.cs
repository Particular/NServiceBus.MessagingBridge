namespace NServiceBus.MessagingBridge.Msmq
{
    using System.Xml.Linq;

    interface IInstanceMappingValidator
    {
        void Validate(XDocument document);
    }
}