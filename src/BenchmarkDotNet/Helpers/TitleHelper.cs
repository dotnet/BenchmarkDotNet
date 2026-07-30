using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// provides the titles that identify a run and every summary it produces
    /// </summary>
    /// <remarks>
    /// the title of a summary is also the base name of every file exported for it, so the titles of the
    /// summaries produced by a single run have to be unique, otherwise the results would overwrite each other #529
    /// </remarks>
    internal static class TitleHelper
    {
        internal const string JoinedSummaryTitle = "BenchmarkRun-joined";
        internal const string MultipleTypesTitle = "BenchmarkRun";

        private const int MinTruncatedLength = 3;

        /// <summary>
        /// the title of the entire run, used as the base name of its log file
        /// </summary>
        internal static string GetRunTitle(BenchmarkRunInfo[] benchmarkRunInfos, int desiredMaxLength = int.MaxValue)
        {
            string? customTitle = benchmarkRunInfos
                .Select(benchmark => benchmark.Config.Title)
                .FirstOrDefault(title => title.IsNotBlank());

            if (customTitle.IsNotBlank())
                return Truncate(Escape(customTitle), desiredMaxLength);

            var uniqueTargetTypes = benchmarkRunInfos
                .SelectMany(info => info.BenchmarksCases.Select(benchmark => benchmark.Descriptor.Type))
                .Distinct()
                .ToArray();

            return Truncate(
                uniqueTargetTypes.Length == 1 ? FolderNameHelper.ToFolderName(uniqueTargetTypes[0]) : MultipleTypesTitle,
                desiredMaxLength);
        }

        /// <summary>
        /// the title of a summary that reports the benchmarks of a single type,
        /// where <paramref name="isTheOnlySummary"/> tells whether the run produces just this one
        /// </summary>
        internal static string GetSummaryTitle(BenchmarkRunInfo benchmarkRunInfo, bool isTheOnlySummary, int desiredMaxLength = int.MaxValue)
        {
            // few types might have the same name: A.Name and B.Name would both report "Name",
            // so we use the namespace-qualified name to tell their results apart #529
            string typeName = FolderNameHelper.ToFolderName(benchmarkRunInfo.Type);
            string? customTitle = benchmarkRunInfo.Config.Title;

            if (customTitle.IsBlank())
                return Truncate(typeName, desiredMaxLength);

            // the title is defined by the config, so every summary of the run would be given the same one
            return Truncate(isTheOnlySummary ? Escape(customTitle) : $"{Escape(customTitle)}-{typeName}", desiredMaxLength);
        }

        /// <summary>
        /// the title of the single summary that joins the results of all the types
        /// </summary>
        internal static string GetJoinedSummaryTitle(string? customTitle, int desiredMaxLength = int.MaxValue)
            => Truncate(customTitle.IsBlank() ? JoinedSummaryTitle : Escape(customTitle), desiredMaxLength);

        // the title becomes a file name, so it can not contain characters that are invalid for a path
        private static string Escape(string title) => FolderNameHelper.ToFolderName((object)title);

        /// <summary>
        /// shortens the title so that the paths of the artifacts named after it don't exceed the limits of the OS
        /// </summary>
        private static string Truncate(string title, int desiredMaxLength)
        {
            if (title.Length <= desiredMaxLength || desiredMaxLength < MinTruncatedLength)
                return title;

            int prefixLength = desiredMaxLength / 2;
            int suffixLength = desiredMaxLength - prefixLength - 1;

            return title.Substring(0, prefixLength) + "-" + title.Substring(title.Length - suffixLength, suffixLength);
        }
    }
}
