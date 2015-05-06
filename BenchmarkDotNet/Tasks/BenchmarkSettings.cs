using System.Collections.Generic;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkSettings
    {
        public const int DefaultWarmupIterationCount = 5;
        public const int DefaultTargetIterationCount = 10;

        public int WarmupIterationCount { get; }
        public int TargetIterationCount { get; }

        public BenchmarkSettings()
        {
        }

        public BenchmarkSettings(int warmupIterationCount, int targetIterationCount)
        {
            WarmupIterationCount = warmupIterationCount;
            TargetIterationCount = targetIterationCount;
        }

        public string ToArgs()
        {
            return $"-w={WarmupIterationCount} -t={TargetIterationCount}";
        }

        public static BenchmarkSettings Parse(string[] args)
        {
            var w = DefaultWarmupIterationCount;
            var t = DefaultTargetIterationCount;
            foreach (var arg in args)
            {
                if (arg.StartsWith("-w="))
                    int.TryParse(arg.Substring(3), out w);
                if (arg.StartsWith("-t="))
                    int.TryParse(arg.Substring(3), out t);
            }
            return new BenchmarkSettings(w, t);
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(WarmupIterationCount), WarmupIterationCount.ToString());
                yield return new BenchmarkProperty(nameof(TargetIterationCount), TargetIterationCount.ToString());
            }
        }

        internal static BenchmarkSettings CreateDefault()
        {
            return new BenchmarkSettings(DefaultWarmupIterationCount, DefaultTargetIterationCount);
        }
    }
}