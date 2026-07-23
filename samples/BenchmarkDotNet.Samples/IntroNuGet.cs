using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Newtonsoft.Json;
using System.IO.Hashing;

namespace BenchmarkDotNet.Samples
{
    /// <summary>
    /// Benchmarks between various versions of a NuGet package
    /// </summary>
    /// <remarks>
    /// Only supported with CsProj toolchains.
    /// </remarks>
    [Config(typeof(Config))]
    public class IntroNuGet
    {
        // Setup your csproj like this:
        /*
        <PropertyGroup>
          <!-- Use 13.0.1 as default package version if not specified -->
          <NewtonsoftJsonVersion Condition="'$(NewtonsoftJsonVersion)' == ''">13.0.1</NewtonsoftJsonVersion>
        </PropertyGroup>
        <ItemGroup>
          <PackageReference Include="Newtonsoft.Json" Version="[$(NewtonsoftJsonVersion)]" />
        </ItemGroup>
        */
        // All versions of the package must be source-compatible with your benchmark code.
        private class Config : ManualConfig
        {
            public Config()
            {
                string[] targetVersions = [
                    "13.0.1",
                    "13.0.2",
                    "13.0.3",
                    "13.0.4",
                ];

                foreach (var version in targetVersions)
                {
                    AddJob(Job.MediumRun
                        .WithMsBuildArguments($"/p:NewtonsoftJsonVersion={version}")
                        .WithId($"v{version}")
                    );
                }
            }
        }

        [Benchmark]
        public void SerializeAnonymousObject()
        {
            JsonConvert.SerializeObject(
                new { hello = "world", price = 1.99, now = DateTime.UtcNow });
        }
    }
}