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
        public void PetMethodConfigsAreRespected()
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
            [OperatingSystemsFilter(allowed: true, PlatformID.Win32NT)]
            public void Method() { }
        }

        public class WithBenchmarkNotAllowedForWindows
        {
            [Benchmark]
            [OperatingSystemsFilter(allowed: false, PlatformID.Win32NT)]
            public void Method() { }
        }
    }
}