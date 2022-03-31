class SomeCommandHandler : IHandleMessages<SomeCommand>
{
    public Task Handle(SomeCommand message, IMessageHandlerContext context)
    {
        Console.WriteLine("Got the message");
        return Task.CompletedTask;
    }
}
