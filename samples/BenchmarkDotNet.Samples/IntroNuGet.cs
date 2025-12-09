using System;
using System.IO.Hashing;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

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
          <!-- Use 8.0.0 as default package version if not specified -->
          <SihVersion Condition="'$(SihVersion)' == ''">8.0.0</SciVersion>
        </PropertyGroup>
        <ItemGroup>
          <PackageReference Include="System.IO.Hashing" Version="$(SihVersion)" />
        </ItemGroup>
        */
        // All versions of the package must be source-compatible with your benchmark code.
        private class Config : ManualConfig
        {
            public Config()
            {
                string[] targetVersions = [
                    "8.0.0",
                    "9.0.0",
                    "10.0.0",
                ];

                foreach (var version in targetVersions)
                {
                    AddJob(Job.MediumRun
                        .WithMsBuildArguments($"/p:SihVersion={version}")
                        .WithId($"v{version}")
                    );
                }
            }
        }

        private static readonly byte[] values;

        static IntroNuGet()
        {
            var rand = new Random(Seed: 0);
            values = new byte[10_000];
            rand.NextBytes(values);
        }

        [Benchmark]
        public void XxHash3Benchmark()
        {
            var results = XxHash3.Hash(values);
        }
    }
}