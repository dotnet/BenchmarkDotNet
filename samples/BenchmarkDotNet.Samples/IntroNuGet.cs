﻿using System;
using System.Collections.Immutable;
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
          <!-- Use 9.0.0 as default package version if not specified -->
          <SciVersion Condition="'$(SciVersion)' == ''">9.0.0</SciVersion>
        </PropertyGroup>
        <ItemGroup>
          <PackageReference Include="System.Collections.Immutable" Version="$(SciVersion)" />
        </ItemGroup>
        */
        // All versions of the package must be source-compatible with your benchmark code.
        private class Config : ManualConfig
        {
            public Config()
            {
                string[] targetVersions = [
                    "9.0.0",
                    "9.0.3",
                    "9.0.5",
                ];

                foreach (var version in targetVersions)
                {
                    AddJob(Job.MediumRun
                        .WithMsBuildArguments($"/p:SciVersion={version}")
                        .WithId($"v{version}")
                    );
                }
            }
        }

        private static readonly Random rand = new Random(Seed: 0);
        private static readonly double[] values = Enumerable.Range(1, 10_000).Select(x => rand.NextDouble()).ToArray();

        [Benchmark]
        public void ToImmutableArrayBenchmark()
        {
            var results = values.ToImmutableArray();
        }
    }
}