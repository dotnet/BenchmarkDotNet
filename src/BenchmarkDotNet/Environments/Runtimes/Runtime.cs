using System;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Environments
{
    public abstract class Runtime : IEquatable<Runtime>
    {
        /// <summary>
        /// Display name
        /// </summary>
        [PublicAPI]
        public string Name { get; }

        /// <summary>
        /// Target Framework Moniker
        /// </summary>
        public RuntimeMoniker RuntimeMoniker { get; }

        /// <summary>
        /// MsBuild Target Framework Moniker, example: net462, netcoreapp2.1
        /// </summary>
        public string MsBuildMoniker { get; }

        public virtual bool IsAOT => false;

        protected Runtime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) throw new ArgumentNullException(nameof(displayName));
            if (string.IsNullOrEmpty(msBuildMoniker)) throw new ArgumentNullException(nameof(msBuildMoniker));

            RuntimeMoniker = runtimeMoniker;
            MsBuildMoniker = msBuildMoniker;
            Name = displayName;
        }

        public override string ToString() => Name;

        public bool Equals(Runtime other)
            => other != null && other.Name == Name && other.MsBuildMoniker == MsBuildMoniker && other.RuntimeMoniker == RuntimeMoniker;

        public override bool Equals(object obj) => obj is Runtime other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Name, MsBuildMoniker, RuntimeMoniker);
    }
}