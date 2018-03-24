using System;

namespace BenchmarkDotNet.Environments
{
    public abstract class Runtime : IEquatable<Runtime>
    {
        /// <summary>
        /// Full .NET Framework (Windows only)
        /// </summary>
        public static readonly Runtime Clr = new ClrRuntime();

        /// <summary>
        /// Mono
        /// See also: http://www.mono-project.com/
        /// </summary>
        public static readonly Runtime Mono = new MonoRuntime();

        /// <summary>
        /// Cross-platform Core CLR runtime
        /// See also: https://docs.microsoft.com/en-us/dotnet/
        /// </summary>
        public static readonly Runtime Core = new CoreRuntime();

        public string Name { get; }

        protected Runtime(string name) => Name = name;

        public override string ToString() => Name;

        public bool Equals(Runtime other) => other != null && other.Name == Name; // for this type this is enough

        public override bool Equals(object obj) => obj is Runtime other && Equals(other);

        public override int GetHashCode() => Name.GetHashCode();
    }

    public class ClrRuntime : Runtime
    {
        public ClrRuntime() : base("Clr")
        {
        }
    }

    public class CoreRuntime : Runtime
    {
        public CoreRuntime() : base("Core")
        {
        }
    }

    public class MonoRuntime : Runtime, IEquatable<MonoRuntime>
    {
        public string CustomPath { get; }

        public MonoRuntime() : base("Mono")
        {
        }

        public MonoRuntime(string name, string customPath) : base(name) => CustomPath = customPath;

        public override bool Equals(object obj) => obj is MonoRuntime other && Equals(other);

        public bool Equals(MonoRuntime other) => other != null && Name == other.Name && CustomPath == other.CustomPath;

        public override int GetHashCode() => Name.GetHashCode() ^ (CustomPath?.GetHashCode() ?? 0);
    }
}