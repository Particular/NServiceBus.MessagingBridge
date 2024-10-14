using System.Collections.Generic;
using System.Linq;

static class StringExtensions
{
    public static string GetClosestMatch(this string input, IEnumerable<string> matchValues)
    {
        var calculator = new Levenshtein(input.ToLower());
        return matchValues
            .OrderBy(x => calculator.DistanceFrom(x.ToLower()))
            .FirstOrDefault();
    }
}
