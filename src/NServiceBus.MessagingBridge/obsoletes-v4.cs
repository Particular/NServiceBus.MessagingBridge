namespace NServiceBus
{
    public partial class BridgeConfiguration
    {
        /// <summary>
        /// Enable ReplyTo address translation on the bridge, which allows seamless retry of messages when endpoints move from one side of the bridge to another
        /// </summary>
        [ObsoleteEx(
        Message = "The bridge now translates ReplyTo addresses for failed messages by default so this method call is no longer necessary.",
        RemoveInVersion = "5.0",
        TreatAsErrorFromVersion = "4.0")]
        public void TranslateReplyToAddressForFailedMessages() => translateReplyToAddressForFailedMessages = true;
    }
}