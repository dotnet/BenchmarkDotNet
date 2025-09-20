using System;
using System.Threading;

namespace BenchmarkDotNet.Helpers
{
    internal static class SleepHelper
    {
        public static void SleepIfPositive(TimeSpan timeSpan)
        {
            if (timeSpan > TimeSpan.Zero)
            {
                Thread.Sleep(timeSpan);
            }
        }
    }
}