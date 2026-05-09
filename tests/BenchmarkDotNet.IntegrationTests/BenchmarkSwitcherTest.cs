using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkSwitcherTest
    {
        internal const string TestCategory = "TestCategory";

        private ITestOutputHelper Output { get; }

        public BenchmarkSwitcherTest(ITestOutputHelper output) => Output = output;

        private IConfig GetRunInProcessConfig()
            => new SingleRunInProcessConfig(Output);

        private IConfig GetRunOutOfProcessConfig()
           => new SingleRunOutOfProcessConfig(Output);

        [FactEnvSpecific(
            "When CommandLineParser wants to display help, it tries to get the Title of the Entry Assembly which is an xunit runner, which has no Title and fails..",
            EnvRequirement.DotNetCoreOnly)]
        public void WhenInvalidCommandLineArgumentIsPassedAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([])
                .Run(["--DOES_NOT_EXIST"], config);

            Assert.Empty(summaries);
            Assert.Contains("Option 'DOES_NOT_EXIST' is unknown.", logger.GetLog());
        }

        [Fact]
        public void WhenUserAsksForInfoAnInfoIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([])
                .Run(["--info"], config);

            Assert.Empty(summaries);
            Assert.Contains(HostEnvironmentInfo.GetInformation(), logger.GetLog());
        }

        [Fact]
        public void WhenInvalidTypeIsProvidedAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([typeof(ClassC)])
                .Run(["--filter", "*"], config);

            Assert.Empty(summaries);
            Assert.Contains(GetValidationErrorForType(typeof(ClassC)), logger.GetLog());
        }

        [Fact]
        public void WhenNoTypesAreProvidedAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([])
                .Run(["--filter", "*"], config);

            Assert.Empty(summaries);
            Assert.Contains("No benchmarks were found.", logger.GetLog());
        }

        [Fact]
        public void WhenFilterReturnsNothingAnErrorMessageIsDisplayedAndNoBenchmarksAreExecuted()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            const string filter = "WRONG";
            var summaries = BenchmarkSwitcher
                .FromTypes([typeof(ClassA), typeof(ClassB)])
                .Run(["--filter", filter], config);

            Assert.Empty(summaries);
            Assert.Contains($"The filter '{filter}' that you have provided returned 0 benchmarks.", logger.GetLog());
        }

        [Fact]
        public void WhenUserAsksToPrintAListWePrintIt()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([typeof(ClassA)])
                .Run(["--list", "flat"], config);

            Assert.Empty(summaries);
            Assert.Contains("BenchmarkDotNet.IntegrationTests.ClassA.Method1", logger.GetLog());
            Assert.Contains("BenchmarkDotNet.IntegrationTests.ClassA.Method2", logger.GetLog());
        }

        [Fact]
        public void WhenUserAsksToPrintAListAndProvidesAFilterWePrintFilteredList()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([typeof(ClassA)])
                .Run(["--list", "flat", "--filter", "*.Method1"], config);

            Assert.Empty(summaries);
            Assert.Contains("BenchmarkDotNet.IntegrationTests.ClassA.Method1", logger.GetLog());
            Assert.DoesNotContain("BenchmarkDotNet.IntegrationTests.ClassA.Method2", logger.GetLog());
        }


        [Fact]
        public void WhenDisableLogFileWeDontWriteToFile()
        {
            var config = GetRunInProcessConfig().WithOptions(ConfigOptions.DisableLogFile);

            string? logFilePath = null;
            try
            {
                var summaries = BenchmarkSwitcher
                    .FromTypes([typeof(ClassA)])
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
            var config = GetRunInProcessConfig();

            string? logFilePath = null;
            try
            {
                var summaries = BenchmarkSwitcher
                    .FromTypes([typeof(ClassA)])
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
            var config = GetRunInProcessConfig();
            var userInteractionMock = new UserInteractionMock(returnValue: []);

            var summaries = new BenchmarkSwitcher(userInteractionMock)
                .With([typeof(WithCategory)])
                .Run([], config);

            Assert.Empty(summaries); // summaries is empty because the returnValue configured for mock returns 0 types
            Assert.Equal(1, userInteractionMock.AskUserCalledTimes);
        }

        [Theory]
        [InlineData("--allCategories")]
        [InlineData("--anyCategories")]
        public void WhenUserProvidesCategoriesWithoutFiltersWeDontAskToChooseBenchmarkJustRunGivenCategories(string categoriesConsoleLineArgument)
        {
            var config = GetRunInProcessConfig();
            var types = new[] { typeof(WithCategory) };
            var userInteractionMock = new UserInteractionMock(returnValue: types);

            var summaries = new BenchmarkSwitcher(userInteractionMock)
                .With(types)
                .Run([categoriesConsoleLineArgument, TestCategory], config);

            Assert.Single(summaries);
            Assert.Equal(0, userInteractionMock.AskUserCalledTimes);
        }

        [Theory]
        [InlineData("--allCategories")]
        [InlineData("--anyCategories")]
        public void WhenUserProvidesCategoriesWithFiltersWeDontAskToChooseBenchmarkJustUseCombinedFilterAndRunTheBenchmarks(string categoriesConsoleLineArgument)
        {
            var config = GetRunInProcessConfig();
            var types = new[] { typeof(WithCategory) };
            var userInteractionMock = new UserInteractionMock(returnValue: types);

            var summaries = new BenchmarkSwitcher(userInteractionMock)
                .With(types)
                .Run([categoriesConsoleLineArgument, TestCategory, "--filter", "nothing"], config);

            Assert.Empty(summaries); // the summaries is empty because the provided filter returns nothing
            Assert.Equal(0, userInteractionMock.AskUserCalledTimes);
        }

        [Fact]
        public void ValidCommandLineArgumentsAreProperlyHandled()
        {
            var config = GetRunOutOfProcessConfig();

            // Don't cover every combination, just pick a complex scenario and check
            // it works end-to-end, i.e. "method=Method1" and "class=ClassB"
            var types = new[] { typeof(ClassA), typeof(ClassB), typeof(NOTIntegrationTests.ClassD) };
            var switcher = new BenchmarkSwitcher(types);

            // BenchmarkSwitcher only picks up config values via the args passed in, not via class annotations (e.g "[DryConfig]")
            var results = switcher.Run(["-j", "Dry", "--filter", "*ClassB.Method4"], config);
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
            var configWithJobDefined = GetRunInProcessConfig().AddExporter(mockExporter);

            var results = switcher.Run(["--filter", "*Method3"], configWithJobDefined);

            Assert.True(mockExporter.exported);

            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job.Id == "Dry")));
        }

        [Fact]
        public void WhenJobIsDefinedViaAttributeAndArgumentsDontContainJobArgumentOnlySingleJobIsUsed()
        {
            var types = new[] { typeof(WithDryAttributeAndCategory) };
            var switcher = new BenchmarkSwitcher(types);
            MockExporter mockExporter = new MockExporter();
            var configWithoutJobDefined = GetRunOutOfProcessConfig().AddExporter(mockExporter);

            var results = switcher.Run(["--filter", "*WithDryAttribute*"], configWithoutJobDefined);

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
            var configWithoutJobDefined = GetRunOutOfProcessConfig().AddExporter(mockExporter);

            var results = switcher.Run(["--filter", "*"], configWithoutJobDefined);

            Assert.True(mockExporter.exported);

            Assert.Single(results);
            Assert.Single(results.SelectMany(r => r.BenchmarksCases));
            Assert.Single(results.SelectMany(r => r.BenchmarksCases.Select(bc => bc.Job)));
            Assert.True(results.All(r => r.BenchmarksCases.All(bc => bc.Job == Job.Default)));
        }

        [Fact]
        public void WhenUserCreatesStaticBenchmarkMethodWeDisplayAnError_FromTypes()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summariesForType = BenchmarkSwitcher
                .FromTypes([typeof(Static.BenchmarkClassWithStaticMethodsOnly)])
                .Run(["--filter", "*"], config);

            Assert.True(summariesForType.Single().HasCriticalValidationErrors);
            Assert.Contains("static", logger.GetLog());
        }

        [Fact]
        public void WhenUserCreatesStaticBenchmarkMethodWeDisplayAnError_FromAssembly()
        {
            var config = GetRunInProcessConfig();
            var logger = config.GetOutputLogger();

            var summariesForAssembly = BenchmarkSwitcher
                .FromAssembly(typeof(Static.BenchmarkClassWithStaticMethodsOnly).Assembly)
                .Run(["--filter", "*"], config);

            Assert.True(summariesForAssembly.Single().HasCriticalValidationErrors);
            Assert.Contains("static", logger.GetLog());
        }

        [FactEnvSpecific("For some reason this test is flaky on Full Framework", EnvRequirement.DotNetCoreOnly)]
        public void WhenUserAddTheResumeAttributeAndRunTheBenchmarks()
        {
            var config = GetRunOutOfProcessConfig().AddJob(Job.Dry);

            var types = new[] { typeof(WithCategory) };
            var switcher = new BenchmarkSwitcher(types);

            // the first run should execute all benchmarks
            Assert.Single(switcher.Run(["--filter", "*WithCategory*"], config));
            // resuming after succesfull run should run nothing
            Assert.Empty(switcher.Run(["--resume", "--filter", "*WithCategory*"], config));
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

        private string GetValidationErrorForType(Type type)
        {
            return $"No [Benchmark] attribute found on '{type.Name}' benchmark case.";
        }
    }

    file static class ExtensionMethods
    {
        public static OutputLogger GetOutputLogger(this IConfig config)
            => (OutputLogger)config.GetLoggers().Single();
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

    [BenchmarkCategory(BenchmarkSwitcherTest.TestCategory)]
    public class WithCategory
    {
        [Benchmark]
        public void Method() { }
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
        public override ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
        {
            exported = true;
            return new();
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
