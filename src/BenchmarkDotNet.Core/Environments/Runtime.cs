namespace BenchmarkDotNet.Environments
{
    public abstract class Runtime
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

        protected Runtime(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }

    public class ClrRuntime : Runtime
    {
        public string Version { get; }

        public ClrRuntime() : base("Clr") { }

        /// <param name="version">YOU PROBABLY DON'T NEED IT, but if you are a .NET Runtime developer..
        /// please set it to particular .NET Runtime version if you want to benchmark it.
        /// BenchmarkDotNet in going to pass `COMPLUS_Version` env var to the process for you.
        /// </param>
        public ClrRuntime(string version) : base("Clr")
        {
            Version = version;
        }
    }

    public class CoreRuntime : Runtime
    {
        public CoreRuntime() : base("Core")
        {
        }
    }

    public class MonoRuntime : Runtime
    {
        public string CustomPath { get; }

        public MonoRuntime() : base("Mono")
        {
        }

        public MonoRuntime(string name, string customPath) : base(name)
        {
            CustomPath = customPath;
        }
    }
}