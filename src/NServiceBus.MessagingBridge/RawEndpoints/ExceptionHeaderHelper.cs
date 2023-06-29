﻿namespace NServiceBus.Raw
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NServiceBus.Logging;

    static class ExceptionHeaderHelper
    {
        const int MAX_LENGTH_TO_PREVENT_TOO_LARGE_HEADERS = 16384;
        static readonly ILog Logger = LogManager.GetLogger(typeof(ExceptionHeaderHelper));

        public static void SetExceptionHeaders(Dictionary<string, string> headers, Exception e)
        {
            headers["NServiceBus.MessagingBridge.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            }

            var exceptionMessage = e.GetMessage();

            if (exceptionMessage.Length > MAX_LENGTH_TO_PREVENT_TOO_LARGE_HEADERS)
            {
                Logger.WarnFormat($"Truncating exception message to {MAX_LENGTH_TO_PREVENT_TOO_LARGE_HEADERS:N0} characters to prevent too large headers to be rejected by transport. Original message:\n{0}", exceptionMessage);
                exceptionMessage = exceptionMessage.Truncate(MAX_LENGTH_TO_PREVENT_TOO_LARGE_HEADERS);
                throw new Exception(exceptionMessage);
            }

            headers["NServiceBus.MessagingBridge.ExceptionInfo.HelpLink"] = e.HelpLink;
            headers["NServiceBus.MessagingBridge.ExceptionInfo.Message"] = exceptionMessage;
            headers["NServiceBus.MessagingBridge.ExceptionInfo.Source"] = e.Source;
            headers["NServiceBus.MessagingBridge.ExceptionInfo.StackTrace"] = e.ToString();
            headers["NServiceBus.MessagingBridge.TimeOfFailure"] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (e.Data == null)
            // ReSharper disable HeuristicUnreachableCode
            {
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            foreach (DictionaryEntry entry in e.Data)
            {
                if (entry.Value == null)
                {
                    continue;
                }
                headers["NServiceBus.MessagingBridge.ExceptionInfo.Data." + entry.Key] = entry.Value.ToString();
            }
        }

        static string GetMessage(this Exception exception)
        {
            try
            {
                return exception.Message;
            }
            catch (Exception)
            {
                return $"Could not read Message from exception type '{exception.GetType()}'.";
            }
        }

        static string Truncate(this string value, int maxLength) =>
            string.IsNullOrEmpty(value)
                ? value
                : (value.Length <= maxLength
                    ? value
                    : value.Substring(0, maxLength));
    }
}