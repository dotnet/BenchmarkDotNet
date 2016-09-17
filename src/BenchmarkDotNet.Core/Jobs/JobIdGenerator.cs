using System;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public static class JobIdGenerator
    {
        public static string GenerateRandomId(Job job)
        {
            int seed = CharacteristicSetPresenter.Display.ToPresentation(job.ToSet()).GetHashCode();
            var random = new Random(seed);
            string id = "";
            for (int i = 0; i < 6; i++)
                id += (char) ('A' + random.Next(26));
            return "Unnamed-" + id;
        }
    }
}