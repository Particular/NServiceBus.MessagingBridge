using System;
using System.Threading.Tasks;
using NServiceBus;

class SomeCommandHandler : IHandleMessages<SomeCommand>
{
    public async Task Handle(SomeCommand message, IMessageHandlerContext context)
    {
        Console.WriteLine("Got the message");
        var replyMsg = new SomeCommand();
        await context.Reply(replyMsg).ConfigureAwait(false);
        //return Task.CompletedTask;
    }
}