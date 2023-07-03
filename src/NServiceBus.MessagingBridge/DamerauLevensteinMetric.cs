using System;
/// <summary>
/// Damerau-Levenshtein distance
/// </summary>
/// <remarks>
/// Source: https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C#
/// </remarks>
class DamerauLevensteinMetric
{
    const int DEFAULTLENGTH = 255;
    int[] currentRow;
    int[] previousRow;
    int[] transpositionRow;

    public DamerauLevensteinMetric()
        : this(DEFAULTLENGTH)
    {
    }

    public DamerauLevensteinMetric(int maxLength)
    {
        currentRow = new int[maxLength + 1];
        previousRow = new int[maxLength + 1];
        transpositionRow = new int[maxLength + 1];
    }

    /// <summary>
    /// Damerau-Levenshtein distance is computed in asymptotic time O((max + 1) * min(first.length(), second.length()))
    /// </summary>
    public int GetDistance(string first, string second, int max = -1)
    {
        int firstLength = first.Length;
        int secondLength = second.Length;

        if (firstLength == 0)
        {
            return secondLength;
        }

        if (secondLength == 0)
        {
            return firstLength;
        }

        if (firstLength > secondLength)
        {
            string tmp = first;
            first = second;
            second = tmp;
            firstLength = secondLength;
            secondLength = second.Length;
        }

        if (max < 0)
        {
            max = secondLength;
        }

        if (secondLength - firstLength > max)
        {
            return max + 1;
        }

        if (firstLength > currentRow.Length)
        {
            currentRow = new int[firstLength + 1];
            previousRow = new int[firstLength + 1];
            transpositionRow = new int[firstLength + 1];
        }

        for (int i = 0; i <= firstLength; i++)
        {
            previousRow[i] = i;
        }

        char lastSecondCh = (char)0;
        for (int i = 1; i <= secondLength; i++)
        {
            char secondCh = second[i - 1];
            currentRow[0] = i;

            // Compute only diagonal stripe of width 2 * (max + 1)
            int from = Math.Max(i - max - 1, 1);
            int to = Math.Min(i + max + 1, firstLength);

            char lastFirstCh = (char)0;
            for (int j = from; j <= to; j++)
            {
                char firstCh = first[j - 1];

                // Compute minimal cost of state change to current state from previous states of deletion, insertion and swapping 
                int cost = firstCh == secondCh ? 0 : 1;
                int value = Math.Min(Math.Min(currentRow[j - 1] + 1, previousRow[j] + 1), previousRow[j - 1] + cost);

                // If there was transposition, take in account its cost 
                if (firstCh == lastSecondCh && secondCh == lastFirstCh)
                {
                    value = Math.Min(value, transpositionRow[j - 2] + cost);
                }

                currentRow[j] = value;
                lastFirstCh = firstCh;
            }
            lastSecondCh = secondCh;

            int[] tempRow = transpositionRow;
            transpositionRow = previousRow;
            previousRow = currentRow;
            currentRow = tempRow;
        }

        return previousRow[firstLength];
    }
}