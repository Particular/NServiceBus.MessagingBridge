using System;
using System.Collections.Generic;
using NServiceBus.Logging;
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
                throw new Exception(new string('Y', 21000))
                {
                    Data = { { "a", new string('Z', 22000) } }
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

            Assert.AreEqual(10000, headers["NServiceBus.MessagingBridge.ExceptionInfo.Message"].Length, "Message");
            Assert.AreEqual(16384, headers["NServiceBus.MessagingBridge.ExceptionInfo.StackTrace"].Length, "Stacktrace");
            Assert.AreEqual(20000, headers["NServiceBus.MessagingBridge.ExceptionInfo.Data.b"].Length, "b");
        }
    }
}