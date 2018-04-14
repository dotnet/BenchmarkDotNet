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

        /// <summary>
        /// Cross-platform .NET Core runtime optimized for ahead of time compilation
        /// See also: https://github.com/dotnet/corert
        /// </summary>
        public static readonly Runtime CoreRT = new CoreRtRuntime();

        public string Name { get; }

        protected Runtime(string name) => Name = name;

        public override string ToString() => Name;

        public bool Equals(Runtime other) => other != null && other.Name == Name; // for this type this is enough

        public override bool Equals(object obj) => obj is Runtime other && Equals(other);

        public override int GetHashCode() => Name.GetHashCode();
    }

    public class ClrRuntime : Runtime, IEquatable<ClrRuntime>
    {
        public string Version { get; }

        public ClrRuntime() : base("Clr") { }

        /// <param name="version">YOU PROBABLY DON'T NEED IT, but if you are a .NET Runtime developer..
        /// please set it to particular .NET Runtime version if you want to benchmark it.
        /// BenchmarkDotNet in going to pass `COMPLUS_Version` env var to the process for you.
        /// </param>
        public ClrRuntime(string version) : this() => Version = version;

        public override bool Equals(object obj) => obj is ClrRuntime other && Equals(other);

        public bool Equals(ClrRuntime other) => other != null && Name == other.Name && Version == other.Version;

        public override int GetHashCode() => Name.GetHashCode() ^ (Version?.GetHashCode() ?? 0);
    }

    public class CoreRuntime : Runtime
    {
        public CoreRuntime() : base("Core")
        {
        }
    }

    public class CoreRtRuntime : Runtime
    {
        public CoreRtRuntime() : base("CoreRT")
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