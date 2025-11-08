using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public static class CombinationsGenerator
{
    public static IEnumerable<int[]> GenerateCombinationsCounts(int length, int maxValue)
    {
        if (length <= 0)
        {
            yield break;
        }

        var baseN = maxValue + 1;
        var total = 1;

        for (var i = 0; i < length; i++)
        {
            total *= baseN;
        }

        for (var i = 0; i < total; i++)
        {
            // ReSharper disable once StackAllocInsideLoop
            Span<int> currentCombination = stackalloc int[length];

            var temp = i;
            for (var j = length - 1; j >= 0; j--)
            {
                currentCombination[j] = temp % baseN;
                temp /= baseN;
            }

            // Copy from Span (stack) to heap-allocated array
            var result = new int[length];
            currentCombination.CopyTo(result);

            yield return result;
        }
    }

    public static IEnumerable<object[]> CombineArguments(params IEnumerable[] argumentSets)
    {
        if (argumentSets.Length == 0)
        {
            yield break;
        }

        IEnumerable<object[]> combinations = [[]];

        foreach (var argumentValues in argumentSets)
        {
            combinations = combinations
                .SelectMany(_ => argumentValues.Cast<object>(), (c, v) => c.Concat([v])
                .ToArray());
        }

        foreach (var combination in combinations)
        {
            yield return combination;
        }
    }
}