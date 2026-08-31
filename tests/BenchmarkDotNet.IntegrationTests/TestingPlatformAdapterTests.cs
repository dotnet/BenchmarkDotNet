#if NETCOREAPP
using BenchmarkDotNet.Detectors;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.IntegrationTests
{
    /// <summary>
    /// Drives the two Microsoft.Testing.Platform probe applications through their command line and asserts on what
    /// BenchmarkDotNet.TestAdapter reports back. Everything here goes through a separate process on purpose: the
    /// adapter's job is to keep a benchmark identifiable and addressable from the outside, and discovery and execution
    /// are two different processes when a test runner drives it.
    /// </summary>
    public class TestingPlatformAdapterTests(ITestOutputHelper output)
    {
        private const string PassingProbes = "BenchmarkDotNet.IntegrationTests.TestingPlatform";
        private const string FailingProbes = "BenchmarkDotNet.IntegrationTests.TestingPlatform.Failures";

        // Both probe projects are single targeted, see their .csproj files.
        private const string ProbeTargetFramework = "net10.0";

        // A run that has to build a benchmark pays for a restore and a build of the generated project.
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

        [Fact]
        public void EveryBenchmarkIsDiscoveredUnderItsOwnName()
        {
            string[] expected =
            [
                // The description of a [Benchmark(Description = ...)] is what a user recognises it by, so it is used
                // instead of the method name. Without one the method name is used, and the parameters are appended to
                // both.
                "DescribedProbe.'A described benchmark'(Size: 1)",
                "DescribedProbe.Undescribed(Size: 1)",

                // A generic benchmark is named after the type arguments it was closed over.
                "GenericProbe<System.Char>.Create",
                "GenericProbe<System.Collections.Generic.List<System.String>>.Create",
                "GenericProbe<System.Int32>.Create",

                "OutOfProcessProbe.Add",
                "SampleBenchmarks.Add(Size: 1)",
                "SampleBenchmarks.Add(Size: 2)",
                "SampleBenchmarks.Multiply(Size: 1)",
                "SampleBenchmarks.Multiply(Size: 2)",
                "SeparatorProbe.Length(Value: \"a/b\")",
            ];

            var discovered = Discover(PassingProbes);

            Assert.Equal(
                expected,
                discovered.Select(test => test.DisplayName.Substring(PassingProbes.Length + 1)).OrderBy(name => name, StringComparer.Ordinal));

            // The platform identifies a node by its uid, so two benchmarks sharing one cannot be told apart.
            Assert.Equal(discovered.Count, discovered.Select(test => test.Uid).Distinct().Count());
        }

        [Fact]
        public void TheUidOfABenchmarkIsTheSameInEveryProcess()
        {
            var first = Discover(PassingProbes).ToDictionary(test => test.Uid, test => test.DisplayName);
            var second = Discover(PassingProbes).ToDictionary(test => test.Uid, test => test.DisplayName);

            Assert.Equal(first, second);
        }

        [Fact]
        public void ABenchmarkCanBeRunByTheUidItWasDiscoveredWith()
        {
            // This is the contract a test runner relies on: it discovers in one process and asks for a uid in another.
            var uid = Discover(PassingProbes)
                .Single(test => test.DisplayName.EndsWith("SampleBenchmarks.Add(Size: 2)", StringComparison.Ordinal))
                .Uid;

            var summary = RunAndSummarize(PassingProbes, "--filter-uid", uid);

            Assert.Equal(1, summary.Total);
            Assert.Equal(1, summary.Succeeded);
            Assert.Equal(0, summary.Failed);
        }

        [Fact]
        public void ATreeNodeFilterMatchesTheCategoriesOfABenchmark()
        {
            // The categories are published as filterable properties, which is what makes this expression work.
            var discovered = Discover(PassingProbes, "--treenode-filter", "/*/*/*/*[Category=Fast]");

            Assert.Equal(
                new[] { "SampleBenchmarks.Add(Size: 1)", "SampleBenchmarks.Add(Size: 2)" },
                discovered.Select(test => test.DisplayName.Substring(PassingProbes.Length + 1)).OrderBy(name => name, StringComparer.Ordinal));
        }

        [Fact]
        public void ATreeNodeFilterMatchesTheClassAndTheMethodOfABenchmark()
        {
            var discovered = Discover(PassingProbes, "--treenode-filter", "/*/*/SampleBenchmarks/Multiply*");

            Assert.Equal(2, discovered.Count);
            Assert.All(discovered, test => Assert.Contains("SampleBenchmarks.Multiply", test.DisplayName, StringComparison.Ordinal));
        }

        [Fact]
        public void ABenchmarkStaysAtTheSameLevelOfTheTreeWhenAParameterContainsTheSeparator()
        {
            // The platform splits the tree path on every '/' and never unescapes it, so a parameter containing one has
            // to be encoded rather than escaped: otherwise the benchmark sits one level deeper and this filter, which
            // matches every other benchmark, would miss it.
            var discovered = Discover(PassingProbes, "--treenode-filter", "/*/*/SeparatorProbe/*");

            Assert.Single(discovered);
            Assert.Contains("SeparatorProbe.Length(Value: \"a/b\")", discovered[0].DisplayName, StringComparison.Ordinal);
        }

        [Fact]
        public void AnOutOfProcessBenchmarkIsBuiltAndRun()
        {
            // The only probe that is not pinned to an in-process toolchain, so the only one that makes the adapter see
            // a real generate/build/execute cycle.
            var summary = RunAndSummarize(PassingProbes, "--treenode-filter", "/*/*/OutOfProcessProbe/*");

            Assert.Equal(1, summary.Total);
            Assert.Equal(1, summary.Succeeded);
            Assert.Equal(0, summary.Failed);
        }

        [Fact]
        public void ABuildFailureIsReportedAsAFailedTest()
        {
            var (summary, standardOutput) = Run(FailingProbes, "--treenode-filter", "/*/*/BuildFailureProbe/*");

            Assert.Equal(1, summary.Total);
            Assert.Equal(1, summary.Failed);
            Assert.Contains("// Build Error: The build of this benchmark always fails, on purpose.", standardOutput, StringComparison.Ordinal);
        }

        [Fact]
        public void BenchmarksSharingAUidAreReportedAsOneFailedTest()
        {
            // Two benchmarks the platform cannot tell apart are published as a single node during discovery, and the
            // collision is reported when they are asked to run.
            Assert.Single(Discover(FailingProbes, "--treenode-filter", "/*/*/CollisionProbe/*"));

            var (summary, standardOutput) = Run(FailingProbes, "--treenode-filter", "/*/*/CollisionProbe/*");

            Assert.Equal(1, summary.Total);
            Assert.Equal(1, summary.Failed);
            Assert.Contains("2 benchmarks are identified as", standardOutput, StringComparison.Ordinal);
        }

        private IReadOnlyList<DiscoveredTest> Discover(string project, params string[] arguments)
        {
            var (exitCode, standardOutput) = Execute(project, ["--list-tests", "json", .. arguments]);

            Assert.Equal(0, exitCode);

            using var document = JsonDocument.Parse(standardOutput);

            return document.RootElement.GetProperty("tests")
                .EnumerateArray()
                .Select(test => new DiscoveredTest(test.GetProperty("uid").GetString()!, test.GetProperty("displayName").GetString()!))
                .ToArray();
        }

        private TestRunSummary RunAndSummarize(string project, params string[] arguments) => Run(project, arguments).Summary;

        private (TestRunSummary Summary, string StandardOutput) Run(string project, params string[] arguments)
        {
            var (_, standardOutput) = Execute(project, arguments);

            return (TestRunSummary.Parse(standardOutput), standardOutput);
        }

        private (int ExitCode, string StandardOutput) Execute(string project, string[] arguments)
        {
            var application = GetProbeApplication(project);
            var startInfo = new ProcessStartInfo(application)
            {
                WorkingDirectory = Path.GetDirectoryName(application),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            // Progress reporting redraws the screen in place, which is noise once the output is redirected.
            foreach (var argument in arguments.Concat(["--no-ansi", "--progress", "off"]))
                startInfo.ArgumentList.Add(argument);

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();

            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (standardOutput) standardOutput.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (standardError) standardError.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"'{Path.GetFileName(application)} {string.Join(" ", arguments)}' did not finish within {Timeout}.");
            }

            // Lets the redirected output be flushed before it is read.
            process.WaitForExit();

            output.WriteLine($"$ {application} {string.Join(" ", startInfo.ArgumentList)}");
            output.WriteLine(standardOutput.ToString());

            if (standardError.Length > 0)
                output.WriteLine($"stderr:{Environment.NewLine}{standardError}");

            return (process.ExitCode, standardOutput.ToString());
        }

        private static string GetProbeApplication(string project)
        {
            // The tests run from <repository>/tests/BenchmarkDotNet.IntegrationTests/bin/<configuration>/<tfm>/, and
            // the probes are built next to them, by the ProjectReferences of this project.
            var binaries = new DirectoryInfo(AppContext.BaseDirectory);
            var configuration = binaries.Parent!.Name;
            var testsFolder = binaries.Parent!.Parent!.Parent!.Parent!.FullName;

            var fileName = OsDetector.IsWindows() ? $"{project}.exe" : project;
            var path = Path.Combine(testsFolder, project, "bin", configuration, ProbeTargetFramework, fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException($"The probe application was not built. Expected it at '{path}'.", path);

            return path;
        }

        private sealed record DiscoveredTest(string Uid, string DisplayName);

        private sealed record TestRunSummary(int Total, int Failed, int Succeeded, int Skipped)
        {
            public static TestRunSummary Parse(string standardOutput)
            {
                // The platform ends a run with a block of "  <name>: <count>" lines under "Test run summary:".
                int Read(string name)
                {
                    var match = Regex.Match(standardOutput, $@"^\s*{name}:\s*(?<count>\d+)\s*$", RegexOptions.Multiline);

                    return match.Success
                        ? int.Parse(match.Groups["count"].Value)
                        : throw new InvalidOperationException($"The test run did not report a '{name}' count.{Environment.NewLine}{standardOutput}");
                }

                return new TestRunSummary(Read("total"), Read("failed"), Read("succeeded"), Read("skipped"));
            }
        }
    }
}
#endif
