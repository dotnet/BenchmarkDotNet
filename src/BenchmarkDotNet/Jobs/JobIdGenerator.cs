using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public static class JobIdGenerator
    {
        private const int JobIdLength = 10;
        private const string JobIdPrefix = "Job-";
        private const string DefaultJobId = "DefaultJob";

        public static string GenerateRandomId(Job job)
        {
            string presentation = CharacteristicSetPresenter.Display.ToPresentation(job);
            if (presentation == string.Empty)
                return DefaultJobId;
            int seed = presentation.GetHashCode();
            var random = new Random(seed);
            string id = string.Empty;
            for (int i = 0; i < 6; i++)
                id += (char)('A' + random.Next(26));
            return JobIdPrefix + id;
        }

        public static bool IsGeneratedJobId(this Job job)
        {
            return job.ResolvedId == DefaultJobId
                || (job.ResolvedId.Length == JobIdLength
                && job.ResolvedId.StartsWith(JobIdPrefix)
                && job.ResolvedId.Replace(JobIdPrefix, string.Empty)
                .ToCharArray()
                .All(x => char.IsLetter(x)));
        }
    }
}