using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Classic
{
    public class Net46Toolchain : Toolchain
    {
        // In case somebody calls ClassicToolchain from .NET Core process 
        // we will build the project as 4.6 because it's the most safe way to do it:
        // * everybody that uses .NET Core must have VS 2015 installed and 4.6 is part of the installation
        // * from 4.6 you can target < 4.6
        private const string TargetFrameworkMoniker = "net46";

        [PublicAPI("Used in auto-generated .exe when this toolchain is set explicitly in Job definition")]
        public Net46Toolchain() : base(
            "Classic",
            new DotNetCliGenerator(
                TargetFrameworkMoniker,
                extraDependencies: "\"frameworkAssemblies\": { \"System.Runtime\": \"4.0.0.0\" },",
                platformProvider: platform => platform.ToConfig(),
                imports: "\"portable-net45+win8\""),
            new DotNetCliBuilder(TargetFrameworkMoniker),
            new Executor())
        {
        }
    }
}