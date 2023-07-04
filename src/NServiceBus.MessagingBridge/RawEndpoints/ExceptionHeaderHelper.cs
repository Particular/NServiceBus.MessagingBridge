namespace NServiceBus.Raw
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;

    using static BridgeHeaders;

    static class ExceptionHeaderHelper
    {
        const int MaxHeaderSize = 32 * 1024; // Half of 64KB to account for unicode + serializer overhead and 64 is AFAIK the lowest limit of all transports
        const int MaxHeaderLengthToPreventTooLargeHeaders = 16 * 1024;

        static readonly ILog Logger = LogManager.GetLogger(typeof(ExceptionHeaderHelper));
        static readonly bool IsDebugEnabled = Logger.IsDebugEnabled;

        public static void SetExceptionHeaders(Dictionary<string, string> headers, Exception e)
        {
            headers[ExceptionInfoExceptionType] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                headers[ExceptionInfoInnerExceptionType] = e.InnerException.GetType().FullName;
            }

            headers[ExceptionInfoHelpLink] = e.HelpLink;
            headers[ExceptionInfoMessage] = e.GetMessage();
            headers[ExceptionInfoSource] = e.Source;
            headers[ExceptionInfoStackTrace] = e.ToString();
            headers[TimeOfFailure] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);

            TruncateHeaderAndWarnIfNeeded(headers, ExceptionInfoMessage);
            TruncateHeaderAndWarnIfNeeded(headers, ExceptionInfoStackTrace);

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

                var value = entry.Value.ToString();
                var headerKey = ExceptionInfoDataPrefix + entry.Key;

                headers[headerKey] = value;
            }

            var totalHeaderSize = headers.Sum(x => x.Key.Length + x.Value?.Length);

            if (IsDebugEnabled)
            {
                Logger.DebugFormat("Total header size is {0:N0} characters", totalHeaderSize);
            }

            foreach (var entry in headers)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                if (entry.Value.Length > MaxHeaderLengthToPreventTooLargeHeaders)
                {
                    Logger.InfoFormat($"Header {{0:N0}} with value length {{1:N0}} exceeds {nameof(MaxHeaderLengthToPreventTooLargeHeaders)} threshold of {MaxHeaderLengthToPreventTooLargeHeaders:N0} characters", entry.Key, entry.Value.Length);
                }
            }

            if (totalHeaderSize > MaxHeaderSize)
            {
                Logger.WarnFormat($"Total header size is {{0:N0}} characters which exceeds {nameof(MaxHeaderSize)} threshold of {MaxHeaderSize:N0} characters. Exceeding this threshold may fail on the transport if the header size exceeds its message size limit", totalHeaderSize);

            }
        }

        static void TruncateHeaderAndWarnIfNeeded(Dictionary<string, string> headers, string headerKeyName)
        {
            var value = headers[headerKeyName];

            if (value.Length > MaxHeaderLengthToPreventTooLargeHeaders)
            {
                Logger.WarnFormat($"Truncating header {headerKeyName} to {MaxHeaderLengthToPreventTooLargeHeaders:N0} characters to prevent too large headers and the message to be rejected by transport. Original value:\n{{0}}", value);
                value = value.Truncate(MaxHeaderLengthToPreventTooLargeHeaders);
                headers[headerKeyName] = value;
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