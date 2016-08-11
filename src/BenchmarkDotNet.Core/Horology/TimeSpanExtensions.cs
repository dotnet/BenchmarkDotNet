using System;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Horology
{
    internal static class TimeSpanExtensions
    {
        /// <summary>
        /// Time in the following format: {th}:{mm}:{ss} ({ts} sec)
        /// 
        /// where
        ///   {th}: total hours (two digits)
        ///   {mm}: minutes (two digits)
        ///   {ss}: seconds (two digits)
        ///   {ts}: total seconds
        /// </summary>
        /// <example>TimeSpan.FromSeconds(2362) -> "00:39:22 (2362 sec)"</example>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToFormattedTotalTime(this TimeSpan time)
        {
            var totalHours = time.Ticks / TimeSpan.TicksPerHour;
            var hhMmSs = $"{totalHours:00}:{time:mm\\:ss}";
            var totalSecs = $"{time.TotalSeconds.ToStr()} sec";
            return $"{hhMmSs} ({totalSecs})";
        }
    }
}