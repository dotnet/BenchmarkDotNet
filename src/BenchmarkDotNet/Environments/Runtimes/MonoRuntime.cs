using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class MonoRuntime : Runtime, IEquatable<MonoRuntime>
    {
        public static readonly MonoRuntime Default = new MonoRuntime("Mono");

        public string CustomPath { get; }

        public string AotArgs { get; }

        public override bool IsAOT => !string.IsNullOrEmpty(AotArgs);

        public string MonoBclPath { get; }

        private MonoRuntime(string name) : base(RuntimeMoniker.Mono, "mono", name) { }

        public MonoRuntime(string name, string customPath) : this(name) => CustomPath = customPath;

        public MonoRuntime(string name, string customPath, string aotArgs, string monoBclPath) : this(name)
        {
            CustomPath = customPath;
            AotArgs = aotArgs;
            MonoBclPath = monoBclPath;
        }

        public override bool Equals(object obj) => obj is MonoRuntime other && Equals(other);

        public bool Equals(MonoRuntime other)
            => base.Equals(other) && Name == other?.Name && CustomPath == other?.CustomPath && AotArgs == other?.AotArgs && MonoBclPath == other?.MonoBclPath;

        public override int GetHashCode()
            => base.GetHashCode() ^ Name.GetHashCode() ^ (CustomPath?.GetHashCode() ?? 0) ^ (AotArgs?.GetHashCode() ?? 0) ^ (MonoBclPath?.GetHashCode() ?? 0);
    }
}
