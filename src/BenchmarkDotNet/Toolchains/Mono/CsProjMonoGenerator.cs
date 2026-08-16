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
            // 2. NU1102 error occurs when running 'dotnet publish' on projects that contain .NET 9.0 or higher. https://github.com/dotnet/BenchmarkDotNet/issues/3000
            return base.GetRuntimeSettings(gcMode, resolver) +
                """
                  <PropertyGroup>
                    <ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles>
                  </PropertyGroup>
                  <PropertyGroup Condition="'$(TargetFramework)' == 'net6.0' or '$(TargetFramework)' == 'net7.0' or '$(TargetFramework)' == 'net8.0'">
                    <UseMonoRuntime>true</UseMonoRuntime>
                  </PropertyGroup>
                """;
        }
    }
}
