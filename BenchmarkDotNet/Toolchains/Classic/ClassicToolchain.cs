using System;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Classic
{
    public class ClassicToolchain : Toolchain
    {
        public static readonly IToolchain Instance = new ClassicToolchain();

        private ClassicToolchain()
#if CLASSIC
            : base("Classic", new RoslynGenerator(), new RoslynBuilder(), new ClassicExecutor())
#else
            : base("Classic", new DotNetCliGenerator(
                      TargetFrameworkMonikerProvider,
                      extraDependencies: "\"frameworkAssemblies\": { \"System.Runtime\": \"4.0.0.0\" }",
                      platformProvider: platform => platform.ToConfig()),
                  new DotNetCliBuilder(TargetFrameworkMonikerProvider),
                  new ClassicExecutor())
#endif
        {
        }

        private static string TargetFrameworkMonikerProvider(Framework framework)
        {
            switch (framework)
            {
                case Framework.Host:
                    throw new ArgumentException("Framework must be set");
                case Framework.V40:
                    return "net40";
                case Framework.V45:
                    return "net45";
                case Framework.V451:
                    return "net451";
                case Framework.V452:
                    return "net452";
                case Framework.V46:
                    return "net46";
                case Framework.V461:
                    return "net461";
                case Framework.V462:
                    return "net462";
                default:
                    throw new ArgumentOutOfRangeException(nameof(framework), framework, null);
            }
        }
    }
}