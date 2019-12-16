using System;
using System.Globalization;
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
        /// <param name="cultureInfo">CultureInfo that will be used for formatting</param>
        /// <returns></returns>
        public static string ToFormattedTotalTime(this TimeSpan time, CultureInfo cultureInfo)
        {
            long totalHours = time.Ticks / TimeSpan.TicksPerHour;
            string hhMmSs = $"{totalHours:00}:{time:mm\\:ss}";
            string totalSecs = $"{time.TotalSeconds.ToString("0.##", cultureInfo)} sec";
            return $"{hhMmSs} ({totalSecs})";
        }
    }
}