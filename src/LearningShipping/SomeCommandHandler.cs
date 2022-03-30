class SomeCommandHandler : IHandleMessages<SomeCommand>
{
    public async Task Handle(SomeCommand message, IMessageHandlerContext context)
    {
        Console.WriteLine("Got the message");
        var replyMsg = new SomeCommand();
        Console.WriteLine("Sending reply message");
        await context.Reply(replyMsg).ConfigureAwait(false);
    }
}
