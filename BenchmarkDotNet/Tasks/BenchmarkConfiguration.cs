using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkConfiguration
    {
        public BenchmarkMode Mode { get; }
        public BenchmarkPlatform Platform { get; }
        public BenchmarkJitVersion JitVersion { get; }
        public BenchmarkFramework Framework { get; }
        public BenchmarkExecutor Executor { get; }
        public BenchmarkRuntime Runtime { get; }
        public string RuntimeVersion { get; }
        public int WarmupIterationCount { get; }
        public int TargetIterationCount { get; }

        public string Caption => Mode + RuntimeSummary + "_" + Platform + "_" + JitVersion + "_NET-" + Framework;

        private string RuntimeSummary
        {
            get
            {
                string result = string.Empty;
                switch (Executor)
                {
                    case BenchmarkExecutor.Classic:
                        switch (Runtime)
                        {
                            case BenchmarkRuntime.Clr:
                                result = string.Empty;
                                break;
                            default:
                                result = Runtime.ToString();
                                break;
                        }
                        break;
                    case BenchmarkExecutor.Dnx:
                        result = "dnx-" + Runtime + "-" + RuntimeVersion;
                        break;
                }
                return (string.IsNullOrEmpty(result) ? string.Empty : "_") + result;
            }
        }

        public BenchmarkConfiguration(
            BenchmarkMode mode,
            BenchmarkPlatform platform,
            BenchmarkJitVersion jitVersion,
            BenchmarkFramework framework,
            BenchmarkExecutor executor,
            BenchmarkRuntime runtime,
            string runtimeVersion,
            int warmupIterationCount,
            int targetIterationCount)
        {
            Mode = mode;
            Platform = platform;
            JitVersion = jitVersion;
            Framework = framework;
            Executor = executor;
            Runtime = runtime;
            RuntimeVersion = runtimeVersion;
            WarmupIterationCount = warmupIterationCount;
            TargetIterationCount = targetIterationCount;
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(Mode), Mode.ToString());
                yield return new BenchmarkProperty(nameof(Platform), Platform.ToString());
                yield return new BenchmarkProperty(nameof(JitVersion), JitVersion.ToString());
                yield return new BenchmarkProperty(nameof(Framework), Framework.ToString());
                yield return new BenchmarkProperty(nameof(Executor), Executor.ToString());
                yield return new BenchmarkProperty(nameof(Runtime), Runtime.ToString());
                yield return new BenchmarkProperty(nameof(RuntimeVersion), RuntimeVersion);
                yield return new BenchmarkProperty(nameof(WarmupIterationCount), WarmupIterationCount.ToString());
                yield return new BenchmarkProperty(nameof(TargetIterationCount), TargetIterationCount.ToString());
            }
        }

        public string ToCtorDefinition()
        {
            var builder = new StringBuilder();
            builder.Append($"{nameof(Mode).ToCamelCase()}: {nameof(BenchmarkMode)}.{Mode}, ");
            builder.Append($"{nameof(Platform).ToCamelCase()}: {nameof(BenchmarkPlatform)}.{Platform}, ");
            builder.Append($"{nameof(JitVersion).ToCamelCase()}: {nameof(BenchmarkJitVersion)}.{JitVersion}, ");
            builder.Append($"{nameof(Framework).ToCamelCase()}: {nameof(BenchmarkFramework)}.{Framework}, ");
            builder.Append($"{nameof(Executor).ToCamelCase()}: {nameof(BenchmarkExecutor)}.{Executor}, ");
            builder.Append($"{nameof(Runtime).ToCamelCase()}: {nameof(BenchmarkRuntime)}.{Runtime}, ");
            builder.Append($"{nameof(RuntimeVersion).ToCamelCase()}: \"{RuntimeVersion}\", ");
            builder.Append($"{nameof(WarmupIterationCount).ToCamelCase()}: {WarmupIterationCount}, ");
            builder.Append($"{nameof(TargetIterationCount).ToCamelCase()}: {TargetIterationCount}");
            return builder.ToString();
        }
    }
}