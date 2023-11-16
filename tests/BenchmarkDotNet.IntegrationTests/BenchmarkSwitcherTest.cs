using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Linq;
using Xunit;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit.Abstractions;
using System.IO;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkSwitcherTest
    {
        internal const string TestCategory = "TestCategory";

        private ITestOutputHelper Output { get; }

        public BenchmarkSwitcherTest(ITestOutputHelper output) => Output = output;

        [FactEnvSpecific(
            "When CommandLineParser wants to display help, it tries to get the Title of the Entry Assembly which is an xunit runner, which has no Title and fails..",
            EnvRequirement.DotNetCoreOnly)]
        public void WhenInvalidCommandLineArgumentIsPassedAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summaries = BenchmarkSwitcher
                .FromTypes(Array.Empty<Type>())
                .Run(new[] { "--DOES_NOT_EXIST" }, config);

            Assert.Empty(summaries);
            Assert.Contains("Option 'DOES_NOT_EXIST' is unknown.", logger.GetLog());
        }

        [Fact]
        public void WhenUserAsksForInfoAnInfoIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summaries = BenchmarkSwitcher
                .FromTypes(Array.Empty<Type>())
                .Run(new[] { "--info" }, config);

            Assert.Empty(summaries);
            Assert.Contains(HostEnvironmentInfo.GetInformation(), logger.GetLog());
        }

        [Fact]
        public void WhenInvalidTypeIsProvidedAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summaries = BenchmarkSwitcher
                .FromTypes(new[] { typeof(ClassC) })
                .Run(new[] { "--filter", "*" }, config);

            Assert.Empty(summaries);
            Assert.Contains("Type BenchmarkDotNet.IntegrationTests.ClassC is invalid.", logger.GetLog());
        }

        [Fact]
        public void WhenNoTypesAreProvidedAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summaries = BenchmarkSwitcher
                .FromTypes(Array.Empty<Type>())
                .Run(new[] { "--filter", "*" }, config);

            Assert.Empty(summaries);
            Assert.Contains("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.", logger.GetLog());
        }

        [Fact]
        public void WhenFilterReturnsNothingAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            const string filter = "WRONG";
            var summaries = BenchmarkSwitcher
                .FromTypes(new[] { typeof(ClassA), typeof(ClassB) })
                .Run(new[] { "--filter", filter }, config);

            Assert.Empty(summaries);
            Assert.Contains($"The filter '{filter}' that you have provided returned 0 benchmarks.", logger.GetLog());
        }

        [Fact]
        public void WhenUserAsksToPrintAListWePrintIt()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summaries = BenchmarkSwitcher
                .FromTypes(new[] { typeof(ClassA) })
                .Run(new[] { "--list", "flat" }, config);

            Assert.Empty(summaries);
            Assert.Contains("BenchmarkDotNet.IntegrationTests.ClassA.Method1", logger.GetLog());
            Assert.Contains("BenchmarkDotNet.IntegrationTests.ClassA.Method2", logger.GetLog());
        }

        [Fact]
        public void WhenUserAsksToPrintAListAndProvidesAFilterWePrintFilteredList()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summaries = BenchmarkSwitcher
                .FromTypes(new[] { typeof(ClassA) })
                .Run(new[] { "--list", "flat", "--filter", "*.Method1" }, config);

            Assert.Empty(summaries);
            Assert.Contains("BenchmarkDotNet.IntegrationTests.ClassA.Method1", logger.GetLog());
            Assert.DoesNotContain("BenchmarkDotNet.IntegrationTests.ClassA.Method2", logger.GetLog());
        }


        [Fact]
        public void WhenDisableLogFileWeDontWriteToFile()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger).WithOptions(ConfigOptions.DisableLogFile).AddJob(Job.Dry);

            string? logFilePath = null;
            try
            {
                var summaries = BenchmarkSwitcher
                    .FromTypes(new[] { typeof(ClassA) })
                    .RunAll(config);

                var summary = summaries.Single();
                logFilePath = summary.LogFilePath;
                Assert.False(File.Exists(logFilePath), $"Logfile '{logFilePath}' should not exist, but it does.");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
        }

        [Fact]
        public void EnsureLogFileIsWritten()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger).AddJob(Job.Dry);

            string? logFilePath = null;
            try
            {
                var summaries = BenchmarkSwitcher
                    .FromTypes(new[] { typeof(ClassA) })
                    .RunAll(config);

                var summary = summaries.Single();
                logFilePath = summary.LogFilePath;
                Assert.True(File.Exists(logFilePath), $"Logfile '{logFilePath}' should exist, but it does not.");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
        }

        [Fact]
        public void WhenUserDoesNotProvideFilterOrCategoriesViaCommandLineWeAskToChooseBenchmark()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);
            var userInteractionMock = new UserInteractionMock(returnValue: Array.Empty<Type>());

            var summaries = new BenchmarkSwitcher(userInteractionMock)
                .With(new[] { typeof(WithDryAttributeAndCategory) })
                .Run(Array.Empty<string>(), config);

            Assert.Empty(summaries); // summaries is empty because the returnValue configured for mock returns 0 types
            Assert.Equal(1, userInteractionMock.AskUserCalledTimes);
        }

        [Theory]
        [InlineData("--allCategories")]
        [InlineData("--anyCategories")]
        public void WhenUserProvidesCategoriesWithoutFiltersWeDontAskToChooseBenchmarkJustRunGivenCategories(string categoriesConsoleLineArgument)
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);
            var types = new[] { typeof(WithDryAttributeAndCategory) };
            var userInteractionMock = new UserInteractionMock(returnValue: types);

            var summaries = new BenchmarkSwitcher(userInteractionMock)
                .With(types)
                .Run(new[] { categoriesConsoleLineArgument, TestCategory }, config);

            Assert.Single(summaries);
            Assert.Equal(0, userInteractionMock.AskUserCalledTimes);
        }

        [Theory]
        [InlineData("--allCategories")]
        [InlineData("--anyCategories")]
        public void WhenUserProvidesCategoriesWithFiltersWeDontAskToChooseBenchmarkJustUseCombinedFilterAndRunTheBenchmarks(string categoriesConsoleLineArgument)
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);
            var types = new[] { typeof(WithDryAttributeAndCategory) };
            var userInteractionMock = new UserInteractionMock(returnValue: types);

            var summaries = new BenchmarkSwitcher(userInteractionMock)
                .With(types)
                .Run(new[] { categoriesConsoleLineArgument, TestCategory, "--filter", "nothing" }, config);

            Assert.Empty(summaries); // the summaries is empty because the provided filter returns nothing
            Assert.Equal(0, userInteractionMock.AskUserCalledTimes);
        }

        [Fact]
        public void ValidCommandLineArgumentsAreProperlyHandled()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            // Don't cover every combination, just pick a complex scenario and check
            // it works end-to-end, i.e. "method=Method1" and "class=ClassB"
            var types = new[] { typeof(ClassA), typeof(ClassB), typeof(NOTIntegrationTests.ClassD) };
            var switcher = new BenchmarkSwitcher(types);

            // BenchmarkSwitcher only picks up config values via the args passed in, not via class annotations (e.g "[DryConfig]")
            var results = switcher.Run(new[] { "-j", "Dry", "--filter", "*ClassB.Method4" }, config);
            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Dry)));
            Assert.True(results.All(r => r.BenchmarksCases.All(b => b.Descriptor.Type.Name == "ClassB" && b.Descriptor.WorkloadMethod.Name == "Method4")));
        }

        [Fact]
        public void WhenJobIsDefinedInTheConfigAndArgumentsDontContainJobArgumentOnlySingleJobIsUsed()
        {
            var types = new[] { typeof(ClassB) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithJobDefined = ManualConfig.CreateEmpty().AddExporter(mockExporter).AddJob(Job.Dry);

            var results = switcher.Run(new[] { "--filter", "*Method3" }, configWithJobDefined);

            Assert.True(mockExporter.exported);

            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Dry)));
        }

        [Fact]
        public void WhenJobIsDefinedViaAttributeAndArgumentsDontContainJobArgumentOnlySingleJobIsUsed()
        {
            var types = new[] { typeof(WithDryAttributeAndCategory) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithoutJobDefined = ManualConfig.CreateEmpty().AddExporter(mockExporter);

            var results = switcher.Run(new[] { "--filter", "*WithDryAttribute*" }, configWithoutJobDefined);

            Assert.True(mockExporter.exported);

            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Dry)));
        }

        [Fact]
        public void JobNotDefinedButStillBenchmarkIsExecuted()
        {
            var types = new[] { typeof(JustBenchmark) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithoutJobDefined = ManualConfig.CreateEmpty().AddExporter(mockExporter);

            var results = switcher.Run(new[] { "--filter", "*" }, configWithoutJobDefined);

            Assert.True(mockExporter.exported);

            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Default)));
        }

        [Fact]
        public void WhenUserCreatesStaticBenchmarkMethodWeDisplayAnError_FromTypes()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summariesForType = BenchmarkSwitcher
                .FromTypes(new[] { typeof(Static.BenchmarkClassWithStaticMethodsOnly) })
                .Run(new[] { "--filter", "*" }, config);

            Assert.True(summariesForType.Single().HasCriticalValidationErrors);
            Assert.Contains("static", logger.GetLog());
        }

        [Fact]
        public void WhenUserCreatesStaticBenchmarkMethodWeDisplayAnError_FromAssembly()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var summariesForAssembly = BenchmarkSwitcher
                .FromAssembly(typeof(Static.BenchmarkClassWithStaticMethodsOnly).Assembly)
                .Run(new[] { "--filter", "*" }, config);

            Assert.True(summariesForAssembly.Single().HasCriticalValidationErrors);
            Assert.Contains("static", logger.GetLog());
        }

        [FactEnvSpecific("For some reason this test is flaky on Full Framework", EnvRequirement.DotNetCoreOnly)]
        public void WhenUserAddTheResumeAttributeAndRunTheBenchmarks()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);

            var types = new[] { typeof(WithDryAttributeAndCategory) };
            var switcher = new BenchmarkSwitcher(types);

            // the first run should execute all benchmarks
            Assert.Single(switcher.Run(new[] { "--filter", "*WithDryAttributeAndCategory*" }, config));
            // resuming after succesfull run should run nothing
            Assert.Empty(switcher.Run(new[] { "--resume", "--filter", "*WithDryAttributeAndCategory*" }, config));
        }

        private class UserInteractionMock : IUserInteraction
        {
            private readonly IReadOnlyList<Type> returnValue;
            internal int PrintNoBenchmarksErrorCalledTimes = 0;
            internal int PrintWrongFilterInfoCalledTimes = 0;
            internal int AskUserCalledTimes = 0;

            internal UserInteractionMock(IReadOnlyList<Type> returnValue) => this.returnValue = returnValue;

            public void PrintNoBenchmarksError(ILogger logger) => PrintNoBenchmarksErrorCalledTimes++;

            public void PrintWrongFilterInfo(IReadOnlyList<Type> allTypes, ILogger logger, string[] userFilters) => PrintWrongFilterInfoCalledTimes++;

            public IReadOnlyList<Type> AskUser(IReadOnlyList<Type> allTypes, ILogger logger)
            {
                AskUserCalledTimes++;

                return returnValue;
            }
        }
    }
}

namespace BenchmarkDotNet.IntegrationTests
{
    public class ClassA
    {
        [Benchmark]
        public void Method1() { }
        [Benchmark]
        public void Method2() { }
    }

    public class ClassB
    {
        [Benchmark]
        public void Method1() { }
        [Benchmark]
        public void Method2() { }
        [Benchmark]
        public void Method3() { }
        [Benchmark]
        public void Method4() { }
    }

    public class ClassC
    {
        // None of these methods are actually Benchmarks!!
        public void Method1() { }
        public void Method2() { }
        public void Method3() { }
    }

    [DryJob]
    [BenchmarkCategory(BenchmarkSwitcherTest.TestCategory)]
    public class WithDryAttributeAndCategory
    {
        [Benchmark]
        public void Method() { }
    }

    public class JustBenchmark
    {
        [Benchmark]
        public void Method() { }
    }

    public class MockExporter : ExporterBase
    {
        public bool exported = false;
        public override void ExportToLog(Summary summary, ILogger logger)
        {
            exported = true;
        }
    }
}

namespace BenchmarkDotNet.NOTIntegrationTests
{
    [DryJob]
    public class ClassD
    {
        [Benchmark]
        public void Method1() { }
        [Benchmark]
        public void Method2() { }
    }
}
