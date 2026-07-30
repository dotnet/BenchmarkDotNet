using BenchmarkDotNet.Running;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by the id of the job they belong to (glob patterns are supported)
    /// </summary>
    public class JobIdFilter : IFilter
    {
        private readonly Regex[] patterns;

        // The available job ids are not known upfront because the jobs defined via attributes are discovered
        // while the benchmark cases are being created. They are recorded here so that when no job matches we
        // can tell the user which ids they could have used instead.
        private readonly HashSet<string> observedJobIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> matchedJobIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public JobIdFilter(string[] jobIds) => patterns = GlobFilter.ToRegex(jobIds);

        /// <summary>
        /// the ids of all the jobs this filter has been asked about.
        /// ImmutableConfigBuilder keeps the filters in an unordered set, so which benchmark cases reach
        /// this filter depends on how it happens to be ordered against the other filters. That makes this
        /// a lower bound of what the user could have typed, which is all we need it for.
        /// </summary>
        internal IReadOnlyCollection<string> ObservedJobIds => observedJobIds;

        /// <summary>
        /// the ids of the jobs that this filter has accepted
        /// </summary>
        internal IReadOnlyCollection<string> MatchedJobIds => matchedJobIds;

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            string jobId = benchmarkCase.Job.ResolvedId;

            observedJobIds.Add(jobId);

            if (!patterns.Any(pattern => pattern.IsMatch(jobId)))
                return false;

            matchedJobIds.Add(jobId);
            return true;
        }
    }
}
