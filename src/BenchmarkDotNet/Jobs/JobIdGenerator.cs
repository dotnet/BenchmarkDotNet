using System;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public static class JobIdGenerator
    {
        public static string GenerateRandomId(Job job)
        {
            string presentation = CharacteristicSetPresenter.Display.ToPresentation(job);
            if (presentation == "")
                return "DefaultJob";
            int seed = GetStableHashCode(presentation);
            var random = new Random(seed);
            string id = "";
            for (int i = 0; i < 6; i++)
                id += (char)('A' + random.Next(26));
            return "Job-" + id;
        }

        // Compute string hash value with DJB2 algorithm.
        private static int GetStableHashCode(string value)
        {
            uint hash = 5381;
            foreach (char c in value)
            {
                hash = ((hash << 5) + hash) + c; // hash * 32 + hash + c
            }
            return unchecked((int)hash);
        }
    }
}