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

            Assert.That(headers["NServiceBus.MessagingBridge.ExceptionInfo.Message"].Length, Is.EqualTo(10000), "Message");
            Assert.That(headers["NServiceBus.MessagingBridge.ExceptionInfo.StackTrace"].Length, Is.EqualTo(16384), "Stacktrace");
            Assert.That(headers["NServiceBus.MessagingBridge.ExceptionInfo.Data.b"].Length, Is.EqualTo(20000), "b");
        }
    }
}