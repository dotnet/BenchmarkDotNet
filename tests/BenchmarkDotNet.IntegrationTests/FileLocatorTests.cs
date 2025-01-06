using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.FileLocators;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Locators;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class FileLocatorTests : BenchmarkTestExecutor
    {
        public FileLocatorTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ExecutionWithoutFileLocatorShouldFail()
        {
            var config = ManualConfig.CreateMinimumViable()
                                     .AddJob(Job.Dry);

            var summary = CanExecute<AssemblyNameIsSetBenchmarks>(config, false);
            Assert.True(summary.Reports.All(r => !r.BuildResult.IsBuildSuccess));
        }

        [Fact]
        public void ExecutionWithFileLocatorShouldSucceed()
        {
            var config = ManualConfig.CreateMinimumViable()
                                     .AddJob(Job.Dry)
                                     .AddFileLocator(new CustomFileLocator());

            CanExecute<AssemblyNameIsSetBenchmarks>(config);
        }

        private class CustomFileLocator : IFileLocator
        {
            public FileLocatorType LocatorType => FileLocatorType.Project;

            public bool TryLocate(FileLocatorArgs args, out FileInfo fileInfo)
            {
                // We manually locate the csproj file, since the default logic of using the AssemblyName does not work
                fileInfo = new FileInfo(Path.Combine(Environment.CurrentDirectory, "../../../../BenchmarkDotNet.IntegrationTests.FileLocators/BenchmarkDotNet.IntegrationTests.FileLocators.csproj"));
                return true;
            }
        }
    }
}