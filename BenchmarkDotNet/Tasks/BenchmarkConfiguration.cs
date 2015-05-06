using System.Collections.Generic;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkConfiguration
    {
        public BenchmarkMode Mode { get; set; }
        public BenchmarkPlatform Platform { get; set; }
        public BenchmarkJitVersion JitVersion { get; set; }
        public BenchmarkFramework Framework { get; set; }

        public string Caption => Mode + "_" + Platform + "_" + JitVersion + "_NET-" + Framework;

        public BenchmarkConfiguration(BenchmarkMode mode, BenchmarkPlatform platform, BenchmarkJitVersion jitVersion, BenchmarkFramework framework)
        {
            Mode = mode;
            Platform = platform;
            JitVersion = jitVersion;
            Framework = framework;
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(Mode), Mode.ToString());
                yield return new BenchmarkProperty(nameof(Platform), Platform.ToString());
                yield return new BenchmarkProperty(nameof(JitVersion), JitVersion.ToString());
                yield return new BenchmarkProperty(nameof(Framework), Framework.ToString());
            }
        }
    }
}