class SomeCommandHandler : IHandleMessages<SomeCommand>
{
    public async Task Handle(SomeCommand message, IMessageHandlerContext context)
    {
        Console.WriteLine("Got the message");
        var replyMsg = new SomeCommand();
        Console.WriteLine("Sending reply message to: {0} on machine: {1}", context.MessageHeaders[Headers.ReplyToAddress], context.MessageHeaders[Headers.OriginatingMachine]);
        await context.Reply(replyMsg).ConfigureAwait(false);
    }
}
