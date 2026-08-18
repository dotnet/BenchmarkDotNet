using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains.Mono
{
    public class MonoGenerator : CsProjGenerator
    {
        public MonoGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string runtimeFrameworkVersion) : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion, true)
        {
        }

        protected override string GetRuntimeSettings(GcMode gcMode, IResolver resolver)
        {
            // NU1102 error occurs when passing /p:UseMonoRuntime=true to the dotnet cli with projects containing .NET 9.0 or higher. #3000
            return base.GetRuntimeSettings(gcMode, resolver) +
                """
                  <PropertyGroup>
                    <UseMonoRuntime>true</UseMonoRuntime>
                  </PropertyGroup>
                """;
        }
    }
}
