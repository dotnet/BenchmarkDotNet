using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
{
    public class ConfigPerMethodTests
    {
        [Fact]
        public void PerMethodConfigsAreRespected()
        {
            var never = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkThatShouldNeverRun));

            Assert.Empty(never.BenchmarksCases);

            var always = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkThatShouldAlwaysRun));

            Assert.NotEmpty(always.BenchmarksCases);
        }

        public class ConditionalRun : FilterConfigBaseAttribute
        {
            public ConditionalRun(bool value) : base(new SimpleFilter(_ => value)) { }
        }

        public class WithBenchmarkThatShouldNeverRun
        {
            [Benchmark]
            [ConditionalRun(false)]
            public void Method() { }
        }

        public class WithBenchmarkThatShouldAlwaysRun
        {
            [Benchmark]
            [ConditionalRun(true)]
            public void Method() { }
        }

        [Fact]
        public void CanEnableOrDisableTheBenchmarkPerOperatingSystem()
        {
            var allowedForWindows = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkAllowedForWindows));
            var notAllowedForWindows = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkNotAllowedForWindows));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.NotEmpty(allowedForWindows.BenchmarksCases);
                Assert.Empty(notAllowedForWindows.BenchmarksCases);
            }
            else
            {
                Assert.Empty(allowedForWindows.BenchmarksCases);
                Assert.NotEmpty(notAllowedForWindows.BenchmarksCases);
            }
        }

        public class WithBenchmarkAllowedForWindows
        {
            [Benchmark]
            [OperatingSystemsFilter(allowed: true, OS.Windows)]
            public void Method() { }
        }

        public class WithBenchmarkNotAllowedForWindows
        {
            [Benchmark]
            [OperatingSystemsFilter(allowed: false, OS.Windows)]
            public void Method() { }
        }

        [Fact]
        public void CanEnableOrDisableTheBenchmarkPerOperatingSystemArchitecture()
        {
            var allowed = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkAllowedForX64));
            var notallowed = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkNotAllowedForX64));

            if (RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                Assert.NotEmpty(allowed.BenchmarksCases);
                Assert.Empty(notallowed.BenchmarksCases);
            }
            else
            {
                Assert.Empty(allowed.BenchmarksCases);
                Assert.NotEmpty(notallowed.BenchmarksCases);
            }
        }

        public class WithBenchmarkAllowedForX64
        {
            [Benchmark]
            [OperatingSystemsArchitectureFilter(allowed: true, Architecture.X64)]
            public void Method() { }
        }

        public class WithBenchmarkNotAllowedForX64
        {
            [Benchmark]
            [OperatingSystemsArchitectureFilter(allowed: false, Architecture.X64)]
            public void Method() { }
        }
    }
}