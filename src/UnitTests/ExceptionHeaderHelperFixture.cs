using System;
using System.Collections.Generic;
using NServiceBus.Raw;
using NUnit.Framework;

public class ExceptionHeaderHelperFixture
{
    [Test]
    public void When_exception_message_is_large_then_it_must_be_truncated()
    {
        var headers = new Dictionary<string, string>();
        try
        {
            try
            {
                throw new Exception(new string('Y', 10000))
                {
                    Data = { { "a", new string('Z', 20000) } }
                };
            }
            catch (Exception inner)
            {

                throw new Exception(new string('X', 10000), inner)
                {
                    Data = { { "b", new string('V', 20000) } }
                };


            }
        }
        catch (Exception outer)
        {
            ExceptionHeaderHelper.SetExceptionHeaders(headers, outer);

            Assert.LessOrEqual(headers["NServiceBus.MessagingBridge.ExceptionInfo.Message"].Length, 16399, "Message");
            Assert.LessOrEqual(headers["NServiceBus.MessagingBridge.ExceptionInfo.StackTrace"].Length, 16399, "Stacktrace");
            Assert.LessOrEqual(headers["NServiceBus.MessagingBridge.ExceptionInfo.Data.b"].Length, 16399, "Stacktrace");
        }
    }
}