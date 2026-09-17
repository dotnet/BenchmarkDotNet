using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Framework;

public class CsProjFrameworkBuilder(DotNetCliSettings settings) : CsProjBuilder(settings)
{
    // .NET Framework builds produce a runnable exe, rather than a dll that the dotnet host runs.
    protected override string GetExecutableExtension() => ".exe";
}
