using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Mathematics.StatisticalTesting;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Analysers
{
    public class RegressionAnalyserTests
    {
        [Fact]
        public void NoDifferenceIfValuesAreTheSame()
        {
            var values = Enumerable.Repeat(100.0, 20).ToArray();
            var summary = CreateSummary(values, values);
            var sut = new RegressionAnalyser(RelativeThreshold.Zero);

            var conclusion = sut.Analyse(summary).Single();
            
            Assert.Equal(ConclusionKind.Hint, conclusion.Kind);
            Assert.Contains("Same", conclusion.Message);
        }
        
        [Fact]
        public void RegressionsAreDetected()
        {
            var @base = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var diff = @base.Select(value => value * 1.03).ToArray();
            
            var summary = CreateSummary(@base, diff);
            var sut = new RegressionAnalyser(new RelativeThreshold(0.02));

            var conclusion = sut.Analyse(summary).Single();
            
            Assert.Equal(ConclusionKind.Error, conclusion.Kind);
            Assert.Contains("Slower", conclusion.Message);
        }
        
        [Fact]
        public void CanCompareDifferentSampleSizes()
        {
            var @base = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var diff = @base
                .Skip(1) // we skip one element to make sure the sample size is different
                .Select(value => value * 1.03).ToArray();
            
            var summary = CreateSummary(@base, diff);
            var sut = new RegressionAnalyser(new RelativeThreshold(0.02));

            var conclusion = sut.Analyse(summary).Single();
            
            Assert.Equal(ConclusionKind.Error, conclusion.Kind);
            Assert.Contains("Slower", conclusion.Message);
        }
        
        [Fact]
        public void ImprovementsreDetected()
        {
            var @base = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var diff = @base.Select(value => value * 0.97).ToArray();
            
            var summary = CreateSummary(@base, diff);
            var sut = new RegressionAnalyser(new RelativeThreshold(0.02));

            var conclusion = sut.Analyse(summary).Single();
            
            Assert.Equal(ConclusionKind.Hint, conclusion.Kind);
            Assert.Contains("Faster", conclusion.Message);
        }
        
        private static Summary CreateSummary(double[] @base, double[] diff)
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(Fake));
            
            var reports = new List<BenchmarkReport>
            {
                CreateReport(runInfo.BenchmarksCases[0], @base),
                CreateReport(runInfo.BenchmarksCases[1], diff)
            };

            return new Summary("mock",
                reports,
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                ManualConfig.CreateEmpty(),
                string.Empty,
                TimeSpan.FromSeconds(1),
                Array.Empty<ValidationError>());
        }

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, double[] values)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            var measurements = values
                .Select((value, index) => new Measurement(1, IterationMode.Workload, IterationStage.Result, index + 1, 1, value))
                .ToList();
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default, Array.Empty<Metric>());
        }

        [ClrJob(baseline: true), CoreJob]
        public class Fake
        {
            [Benchmark] public void Nothing() { }
        }
    }
}