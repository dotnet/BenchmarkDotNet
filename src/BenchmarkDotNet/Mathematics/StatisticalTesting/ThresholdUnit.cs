using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public enum ThresholdUnit
    {
        Ratio,
        Nanoseconds,
        Microseconds,
        Milliseconds,
        Seconds,
        Minutes
    }

    internal static class ThresholdUnitExtensions
    {
        private static readonly IReadOnlyDictionary<ThresholdUnit, string> UnitToShortName = new Dictionary<ThresholdUnit, string>()
        {
            { ThresholdUnit.Ratio, "%" },
            { ThresholdUnit.Nanoseconds, "ns" },
            { ThresholdUnit.Microseconds, "us" },
            { ThresholdUnit.Milliseconds, "ms" },
            { ThresholdUnit.Seconds, "s" },
            { ThresholdUnit.Minutes, "m" },
        };

        internal static readonly IReadOnlyDictionary<string, ThresholdUnit> ShortNameToUnit = UnitToShortName.ToDictionary(pair => pair.Value, pair => pair.Key); 

        internal static string ToShortName(this ThresholdUnit thresholdUnit) => UnitToShortName[thresholdUnit];
    }
}