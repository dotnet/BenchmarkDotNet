using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains.Mono
{
    internal sealed class CsProjMonoGenerator(MonoCoreSettings settings) : CsProjGenerator(settings)
    {
        protected override string GetRuntimeSettings(GcMode gcMode, IResolver resolver)
        {
            // Workaround for following issues.
            // 1. 'Found multiple publish output files with the same relative path' error
            // 2. NU1102 error occurs when passing /p:UseMonoRuntime=true to the dotnet cli with projects containing .NET 9.0 or higher. #3000
            return base.GetRuntimeSettings(gcMode, resolver) +
                """
                  <PropertyGroup>
                    <ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles>
                    <UseMonoRuntime>true</UseMonoRuntime>
                  </PropertyGroup>
                """;
        }
    }
}
