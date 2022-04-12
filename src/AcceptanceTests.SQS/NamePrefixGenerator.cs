﻿using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

class NamePrefixGenerator
{

    static readonly string NamePrefix = $"AT{Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "").ToUpperInvariant()}";

    public static string GetNamePrefix()
    {
        var testRunId = TestContext.CurrentContext.Test.ID;
        return NamePrefix + testRunId;
    }
}
