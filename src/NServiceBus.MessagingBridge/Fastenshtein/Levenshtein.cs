﻿// Source: https://github.com/DanHarltey/Fastenshtein/blob/master/src/Fastenshtein/Levenshtein.cs
/// <summary>
/// Measures the difference between two strings.
/// Uses the Levenshtein string difference algorithm.
/// </summary>
class Levenshtein
{
    /*
     * WARRING this class is performance critical (Speed).
     */

    readonly string storedValue;
    readonly int[] costs;

    /// <summary>
    /// Creates a new instance with a value to test other values against
    /// </summary>
    /// <param Name="value">Value to compare other values to.</param>
    public Levenshtein(string value)
    {
        storedValue = value;
        // Create matrix row
        costs = new int[storedValue.Length];
    }

    /// <summary>
    /// gets the length of the stored value that is tested against
    /// </summary>
    public int StoredLength => storedValue.Length;

    /// <summary>
    /// Compares a value to the stored value.
    /// Not thread safe.
    /// </summary>
    /// <returns>Difference. 0 complete match.</returns>
    public int DistanceFrom(string value)
    {
        if (costs.Length == 0)
        {
            return value.Length;
        }

        // Add indexing for insertion to first row
        for (int i = 0; i < costs.Length;)
        {
            costs[i] = ++i;
        }

        for (int i = 0; i < value.Length; i++)
        {
            // cost of the first index
            int cost = i;
            int previousCost = i;

            // cache value for inner loop to avoid index lookup and bonds checking, profiled this is quicker
            char value1Char = value[i];

            for (int j = 0; j < storedValue.Length; j++)
            {
                int currentCost = cost;

                // assigning this here reduces the array reads we do, improvement of the old version
                cost = costs[j];

                if (value1Char != storedValue[j])
                {
                    if (previousCost < currentCost)
                    {
                        currentCost = previousCost;
                    }

                    if (cost < currentCost)
                    {
                        currentCost = cost;
                    }

                    ++currentCost;
                }

                /*
                 * Improvement on the older versions.
                 * Swapping the variables here results in a performance improvement for modern intel CPU’s, but I have no idea why?
                 */
                costs[j] = currentCost;
                previousCost = currentCost;
            }
        }

        return costs[costs.Length - 1];
    }
}