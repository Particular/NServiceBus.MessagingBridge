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
        static readonly bool IsInfoEnabled = Logger.IsInfoEnabled;

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

            if (e.Data == null)
            {
                return;
            }

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

            TruncateHeaderAndWarnIfNeeded(headers, ExceptionInfoMessage);
            TruncateHeaderAndWarnIfNeeded(headers, ExceptionInfoStackTrace);

            if (IsInfoEnabled)
            {
                foreach (var entry in headers)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    if (entry.Value.Length > MaxHeaderLengthToPreventTooLargeHeaders)
                    {
                        Logger.InfoFormat("Header {0:N0} with value length {1:N0} exceeds threshold of {2:N0} characters", entry.Key, entry.Value.Length, MaxHeaderLengthToPreventTooLargeHeaders);
                    }
                }
            }

            if (IsDebugEnabled)
            {
                Logger.DebugFormat("Total size of header of keys and values is {0:N0} characters", totalHeaderSize);
            }

            if (totalHeaderSize > MaxHeaderSize)
            {
                Logger.WarnFormat("Total size of header of keys and values is {0:N0} characters exceeds threshold of {1:N0} characters", totalHeaderSize, MaxHeaderSize);
            }
        }

        static void TruncateHeaderAndWarnIfNeeded(Dictionary<string, string> headers, string headerKeyName)
        {
            var value = headers[headerKeyName];

            if (value.Length > MaxHeaderLengthToPreventTooLargeHeaders)
            {
                Logger.WarnFormat("Truncating header {0} from {1:N0} to {2:N0} characters. Original value:\n{3}", headerKeyName, value.Length, MaxHeaderLengthToPreventTooLargeHeaders, value);
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