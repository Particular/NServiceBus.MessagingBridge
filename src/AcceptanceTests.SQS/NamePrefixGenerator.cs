using System;
using System.Text.RegularExpressions;

class NamePrefixGenerator
{

    static readonly string NamePrefix = $"AT{Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "").ToUpperInvariant()}";

    public static string GetNamePrefix()
    {
        return NamePrefix;
    }
}
