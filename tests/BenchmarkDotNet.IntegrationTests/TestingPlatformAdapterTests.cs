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
        private const string UnoptimizedProbes = "BenchmarkDotNet.IntegrationTests.TestingPlatform.Unoptimized";
        private const string InternalsProbe = "BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals";

        // Every probe project is single targeted, see their .csproj files.
        private const string ProbeTargetFramework = "net10.0";

        // A run that has to build a benchmark pays for a restore and a build of the generated project.
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

        [Fact]
        public void EveryBenchmarkIsDiscoveredUnderItsOwnName()
        {
            string[] expected =
            [
                // The values of this one are IAsyncDisposable and not IDisposable, which is what an async
                // [ParamsSource] can produce since #3248.
                "AsyncDisposableProbe.Identity(Value: async-1)",
                "AsyncDisposableProbe.Identity(Value: async-2)",

                "BracketProbe.Length(Value: \"[Dry]\")",
                "CategoryProbe.Identity",

                // The description of a [Benchmark(Description = ...)] is what a user recognises it by, so it is
                // used instead of the method name, and spelled the way it was written. Descriptor quotes a
                // description containing a space so that BenchmarkDotNet's own --filter can delimit it, which an IDE
                // label has no use for. Without a description the method name is used, and the parameters are
                // appended to both.
                "DescribedProbe.A described benchmark(Size: 1)",
                "DescribedProbe.Undescribed(Size: 1)",

                "DisposableProbe.Identity(Value: tracked-1)",
                "DisposableProbe.Identity(Value: tracked-2)",
                "DisposableProbe.Identity(Value: tracked-3)",

                "FreshValueProbe.Identity(Value: fresh-1)",
                "FreshValueProbe.Identity(Value: fresh-2)",

                // A generic benchmark is named after the type arguments it was closed over.
                "GenericProbe<System.Char>.Create",
                "GenericProbe<System.Collections.Generic.List<System.String>>.Create",
                "GenericProbe<System.Int32>.Create",

                "NestedProbe.Inner.Identity",
                "OutOfProcessProbe.Add",
                "SampleBenchmarks.Add(Size: 1)",
                "SampleBenchmarks.Add(Size: 2)",
                "SampleBenchmarks.Multiply(Size: 1)",
                "SampleBenchmarks.Multiply(Size: 2)",
                "SeparatorProbe.Length(Value: \"a/b\")",
            ];

            var discovered = Discover(PassingProbes);

            // InvalidConfigProbe's benchmarks are deliberately absent: their [Config] cannot be constructed, and a
            // type whose attributes cannot be read is dropped rather than allowed to abort the whole discovery.
            Assert.Equal(
                expected,
                discovered.Select(test => test.DisplayName.Substring(PassingProbes.Length + 1)).OrderBy(name => name, StringComparer.Ordinal));

            // The platform identifies a node by its uid, so two benchmarks sharing one cannot be told apart.
            Assert.Equal(discovered.Count, discovered.Select(test => test.Uid).Distinct().Count());
        }

        [Fact]
        public void TheTypeOfABenchmarkIsIdentifiedByItsEcmaName()
        {
            // Microsoft.Testing.Platform documents TestMethodIdentifierProperty as ECMA-335, which is the form a
            // test runner - Visual Studio's Test Explorer above all - matches a type by. A generic type is named
            // after its arity there, and its arguments are no part of it.
            var generic = Discover(PassingProbes, "--treenode-filter", "/*/*/GenericProbe*/*");

            Assert.Equal(3, generic.Count);
            Assert.All(generic, test => Assert.Equal("GenericProbe`1", test.TypeName));

            // The arguments are still what tells one closed generic from another, in the name the user reads.
            Assert.Equal(3, generic.Select(test => test.DisplayName).Distinct(StringComparer.Ordinal).Count());

            // A nested type is qualified by its declaring types rather than by its namespace, which the property
            // carries separately.
            var nested = Discover(PassingProbes, "--treenode-filter", "/*/*/NestedProbe*/*");

            Assert.Equal("NestedProbe+Inner", Assert.Single(nested).TypeName);
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
        public void ATreeNodeFilterMatchesTheCategoriesOfACustomCategoryDiscoverer()
        {
            // The node has to carry the categories the config resolved, not the ones the default discoverer finds:
            // this category is produced by an ICategoryDiscoverer and exists on no [BenchmarkCategory] anywhere.
            var discovered = Discover(PassingProbes, "--treenode-filter", "/*/*/*/*[Category=DiscoveredIdentity]");

            Assert.Equal(
                new[] { "CategoryProbe.Identity" },
                discovered.Select(test => test.DisplayName.Substring(PassingProbes.Length + 1)));
        }

        [Fact]
        public void ParameterValuesAreDisposedWhenBenchmarksAreOnlyListed()
        {
            // Listing runs nothing, so BenchmarkDotNet disposes nothing: every value the enumeration created is the
            // adapter's to dispose.
            Assert.Equal(
                "created=3 disposed=3",
                ReadDisposalReport(PassingProbes, "disposable-probe.txt", () => Discover(PassingProbes)));
        }

        [Fact]
        public void ParameterValuesThatAreOnlyAsyncDisposableAreDisposedToo()
        {
            // An async [ParamsSource] can hand back a value that implements IAsyncDisposable and not IDisposable.
            // Disposal goes through ParameterInstance, which knows both, rather than casting the value to
            // IDisposable - which would drop these on the floor and leave them to the finalizer.
            Assert.Equal(
                "created=2 disposed=2",
                ReadDisposalReport(PassingProbes, "async-disposable-probe.txt", () => Discover(PassingProbes)));
        }

        [Fact]
        public void ParameterValuesSurviveADiscoveryWhenTheSameProcessRunsThemAfterwards()
        {
            // Server mode is how Visual Studio and the Visual Studio Code Test Explorer drive the platform: one
            // process serves the discovery and then the runs. Every request enumerates the assembly again, and a
            // source backed by a cached collection hands back the very same values, so disposing them when the
            // discovery request ends would leave the run executing against disposed objects - which DisposableProbe
            // turns into an ObjectDisposedException rather than letting it pass unnoticed.
            IReadOnlyList<TestingPlatformServerModeSession.ServerNode> discovered = [];
            IReadOnlyList<TestingPlatformServerModeSession.ServerNode> ran = [];

            var report = ReadDisposalReport(
                PassingProbes,
                "disposable-probe.txt",
                () => (discovered, ran) = TestingPlatformServerModeSession.DiscoverThenRun(
                    GetProbeApplication(PassingProbes),
                    "DisposableProbe.Identity(Value: tracked",
                    Timeout));

            Assert.NotEmpty(discovered);
            Assert.Equal(3, ran.Count);
            Assert.All(ran, node => Assert.Equal("passed", node.ExecutionState));

            // Once each: BenchmarkDotNet disposes what it ran, and the adapter must not have done so beforehand.
            Assert.Equal("created=3 disposed=3", report);
        }

        [Fact]
        public void ParameterValuesOfBenchmarksNoRequestRanAreDisposedWhenTheApplicationEnds()
        {
            // The mirror image of the test above: the values of every benchmark that neither request ran are the
            // adapter's to dispose, and holding them for the application rather than for the request must not turn
            // into either a leak or a second disposal.
            var report = ReadDisposalReport(
                PassingProbes,
                "async-disposable-probe.txt",
                () => TestingPlatformServerModeSession.DiscoverThenRun(
                    GetProbeApplication(PassingProbes),
                    "DisposableProbe.Identity(Value: tracked",
                    Timeout));

            Assert.Equal("created=2 disposed=2", report);
        }

        [Fact]
        public void ParameterValuesOfASourceThatConstructsPerReadAreDisposedAsRequestsGoBy()
        {
            // FreshValueProbe's source hands back new values on every read, so nothing one request enumerated can
            // ever be reused by the next. Holding them for the whole session would turn a long Test Explorer session
            // into the handle leak the disposal exists to prevent: they have to go as soon as the following request
            // shows they did not come back. The probe reports what had been disposed by the time each read happened.
            var report = ReadDisposalReport(
                PassingProbes,
                "fresh-value-probe.txt",
                () => TestingPlatformServerModeSession.DiscoverThenRun(
                    GetProbeApplication(PassingProbes),
                    "DisposableProbe.Identity(Value: tracked",
                    Timeout,
                    discoverAgain: true));

            Assert.Equal(
                [
                    "read=1 created=2 disposed=0",

                    // The values of the first request are only known to be unreusable once this read did not hand
                    // them back, which is after it.
                    "read=2 created=4 disposed=0",

                    // By the third request they are gone, and so on: the session holds one request's worth.
                    "read=3 created=6 disposed=2",
                    "exit created=6 disposed=6",
                ],
                report.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
        }

        [Fact]
        public void AnAssemblyWideValidationWarningIsReportedOncePerNode()
        {
            // GenericBenchmarksValidator looks at the whole assembly, however BenchmarkDotNet runs the validators once
            // per benchmark type, so an unreadable type is reported again for every type that runs. An error that
            // names no benchmark case is put on every node, so without deduplication N types leave N copies of the
            // same warning on each of the N types' nodes.
            var (_, ran) = TestingPlatformServerModeSession.DiscoverThenRun(
                GetProbeApplication(PassingProbes),
                "Probe.Identity",
                Timeout);

            // The dedup only does anything when more than one BenchmarkRunInfo is validated, so the benchmarks that
            // ran have to span several types for this to be exercising it at all - which counting nodes would not say.
            var types = ran
                .Select(node => node.DisplayName.Substring(PassingProbes.Length + 1).Split('.')[0])
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            Assert.True(types.Length >= 2, $"Expected several benchmark types to run, but only {string.Join(", ", types)} did.");
            Assert.All(ran, node => Assert.Equal("passed", node.ExecutionState));
            Assert.All(
                ran,
                node => Assert.Single(Regex.Matches(node.StandardOutput, "WithAbstractConfig was ignored")));
        }

        [Fact]
        public void ParameterValuesAreDisposedWhenBenchmarkDotNetBailsOutOnValidation()
        {
            // The unoptimized probe application fails JitOptimizationsValidator, which is critical: BenchmarkRunnerClean
            // returns before the try whose finally disposes the values it was handed, so nothing disposes them. The
            // adapter must not take "handed to BenchmarkDotNet" for "disposed by BenchmarkDotNet" - that assumption
            // would leave exactly these values, of a run that never started, to the finalizer for good.
            TestRunSummary? summary = null;

            var report = ReadDisposalReport(
                UnoptimizedProbes,
                "unoptimized-probe.txt",
                () => summary = RunAndSummarize(UnoptimizedProbes, "--treenode-filter", "/*/*/SharedValueProbe/*"));

            Assert.Equal(2, summary!.Total);
            Assert.Equal(2, summary.Failed);
            Assert.Equal("created=4 disposed=4", report);
        }

        [Fact]
        public void ParameterValuesAreDisposedWhenOnlyOneBenchmarkOfASetIsRun()
        {
            // BenchmarkDotNet only disposes the case it was handed, so the two that were filtered out would leak. A
            // value shared with the case that runs must not be disposed early either, which the count would catch as
            // a disposal too many.
            var uid = Discover(PassingProbes)
                .Single(test => test.DisplayName.EndsWith("DisposableProbe.Identity(Value: tracked-1)", StringComparison.Ordinal))
                .Uid;

            var report = ReadDisposalReport(
                PassingProbes,
                "disposable-probe.txt",
                () => RunAndSummarize(PassingProbes, "--filter-uid", uid));

            Assert.Equal("created=3 disposed=3", report);
        }

        [Fact]
        public void OutOfProcessBenchmarksAreHiddenWhenTheAssemblyIsNotOptimized()
        {
            // The point of the unoptimized probe application: a benchmark that would leave the process is hidden, so
            // that it can be debugged from a test runner. DroppedProbe has no other job and disappears entirely,
            // SharedValueProbe keeps its in-process cases - which is also why the job is no part of their names.
            var discovered = Discover(UnoptimizedProbes);

            Assert.Equal(
                new[] { "SharedValueProbe.Length(Value: shared-1)", "SharedValueProbe.Length(Value: shared-2)" },
                discovered.Select(test => test.DisplayName.Substring(UnoptimizedProbes.Length + 1)).OrderBy(name => name, StringComparer.Ordinal));
        }

        [Fact]
        public void ParameterValuesAreDisposedWhenBenchmarksAreHiddenByAnUnoptimizedAssembly()
        {
            // The benchmarks hidden above are never handed to BenchmarkDotNet by either adapter, so the values they
            // own are the enumeration's to dispose: the two of DroppedProbe are unreachable from anything that
            // survives. The two of SharedValueProbe are shared with cases that do survive, so disposing them here
            // would be a disposal too many, which the count catches just as well as a leak.
            var report = ReadDisposalReport(UnoptimizedProbes, "unoptimized-probe.txt", () => Discover(UnoptimizedProbes));

            Assert.Equal("created=4 disposed=4", report);
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
        public void ABenchmarkIsAddressableWhenItsPathContainsAPropertyFilterDelimiter()
        {
            // A TreeNodeFilter reads '[' and ']' as the delimiters of a property filter, so a segment carrying them
            // has to be encoded rather than left to be parsed - unlike the parentheses around the parameters, which a
            // filter escapes with a backslash. That is true of a parameter that contains them...
            var byParameter = Discover(PassingProbes, "--treenode-filter", @"/*/*/BracketProbe/Length\(Value: ""%5BDry%5D""\)*");

            Assert.Single(byParameter);
            Assert.Contains("BracketProbe.Length(Value: \"[Dry]\")", byParameter[0].DisplayName, StringComparison.Ordinal);

            // ...and of the job that every leaf ends in, which is what an exact path would otherwise trip over. The
            // trailing wildcard stands in for the job name, so that this does not pin how a job is displayed.
            var byJob = Discover(PassingProbes, "--treenode-filter", @"/*/*/BracketProbe/Length\(Value: ""%5BDry%5D""\) %5B*");

            Assert.Single(byJob);
            Assert.Equal(byParameter[0].Uid, byJob[0].Uid);
        }

        [Fact]
        public void ADiscoveryWithAnUnrecognisedFilterListsEveryBenchmarkAndSaysSo()
        {
            // Microsoft.Testing.Platform 2.3.3 has no filter the adapter does not handle, and the extension point for
            // adding one is internal to it, so this branch is unreachable from a real test host - FilterProbe drives
            // the framework itself to reach it. Discovery runs nothing, so listing too much is the cheap mistake and
            // reporting nothing is the expensive one; the warning is what makes the wrong list visible.
            var report = RunInternalsProbe();

            Assert.Contains(
                report.Discover,
                line => line.StartsWith("output ", StringComparison.Ordinal)
                    && line.Contains("does not recognise", StringComparison.Ordinal)
                    && line.Contains("UnrecognisedFilter", StringComparison.Ordinal));

            // Everything the probe assembly declares, rather than a count that a benchmark added to it would break.
            Assert.NotEmpty(report.Discovered);
            Assert.Contains("complete True", report.Discover);
        }

        [Fact]
        public void ARunWithAnUnrecognisedFilterIsRefusedWithAFailedNodePerBenchmark()
        {
            // The other half of the same branch: a run cannot list too much, because it would spend the machine's next
            // hour on it. Refusing by throwing would be invisible - the request is completed before the exception is
            // observed - so every benchmark it could have selected is reported failed instead, which is where an IDE
            // shows it.
            var report = RunInternalsProbe();

            var failed = report.Run
                .Where(line => line.StartsWith("failed(", StringComparison.Ordinal))
                .ToArray();

            // Every benchmark the same filter listed during discovery is reported, so that none of them is left
            // looking like it was quietly skipped.
            Assert.Equal(report.Discovered.Length, failed.Length);
            Assert.All(failed, line => Assert.Contains("does not support", line, StringComparison.Ordinal));
            Assert.All(failed, line => Assert.Contains("UnrecognisedFilter", line, StringComparison.Ordinal));

            // Every one of them was reported as started too, and the request finished rather than throwing.
            Assert.Equal(failed.Length, report.Run.Count(line => line.StartsWith("in-progress ", StringComparison.Ordinal)));
            Assert.DoesNotContain(report.Run, line => line.StartsWith("threw ", StringComparison.Ordinal));
            Assert.Contains("complete True", report.Run);
        }

        [Fact]
        public void ParameterValuesOfARequestStillInFlightAreDisposedWhenTheApplicationEnds()
        {
            // A request hands its values over by completing. One that never gets there - the client sent `exit`, or
            // the IDE cancelled, while it was still in flight - leaves them reachable from nothing else, and a value
            // left to the finalizer instead is the dotnet/BenchmarkDotNet#1383 hang this disposal exists to prevent.
            var report = RunInternalsProbe();

            Assert.Equal("created=2 disposed=2", Assert.Single(report.Abandoned));
        }

        /// <summary>
        /// Runs the application that drives the adapter's platform types directly, and splits what it reported into
        /// its sections.
        /// </summary>
        /// <returns>The lines of each section, and the benchmarks the discovery request listed.</returns>
        private InternalsReport RunInternalsProbe()
        {
            var (exitCode, standardOutput) = Execute(InternalsProbe, []);

            Assert.Equal(0, exitCode);

            var lines = standardOutput.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

            var abandonedStart = Array.IndexOf(lines, "== abandoned");
            var discoverStart = Array.IndexOf(lines, "== discover");
            var runStart = Array.IndexOf(lines, "== run");
            var end = Array.IndexOf(lines, "== done");

            Assert.True(
                abandonedStart >= 0 && discoverStart > abandonedStart && runStart > discoverStart && end > runStart,
                $"The internals probe did not report every section:{Environment.NewLine}{standardOutput}");

            var discover = lines[(discoverStart + 1)..runStart];

            return new InternalsReport(
                lines[(abandonedStart + 1)..discoverStart],
                discover,
                lines[(runStart + 1)..end],
                discover.Where(line => line.StartsWith("discovered ", StringComparison.Ordinal)).ToArray());
        }

        private sealed record InternalsReport(string[] Abandoned, string[] Discover, string[] Run, string[] Discovered);

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

        [Fact]
        public void BenchmarksSharingAUidThroughTheirDescriptionAreReportedWithTheirMethodNames()
        {
            // Nothing here is parameterized: what collides is the description of one benchmark against the method
            // name of the other, so the message has to name the two methods and point at the description.
            var (summary, standardOutput) = Run(FailingProbes, "--treenode-filter", "/*/*/DescriptionCollisionProbe/*");

            Assert.Equal(1, summary.Total);
            Assert.Equal(1, summary.Failed);
            Assert.Contains("none of them were run: Described, Twin.", standardOutput, StringComparison.Ordinal);
            Assert.Contains("[Benchmark(Description = \"...\")]", standardOutput, StringComparison.Ordinal);
        }

        /// <summary>
        /// Runs the probe application and reads back what it reported about the disposal of its parameter values.
        /// </summary>
        /// <remarks>
        /// The counts are written to a file rather than to the output, because the discovery output is parsed as json.
        /// </remarks>
        /// <param name="project">The probe application that writes the counts.</param>
        /// <param name="reportFileName">The name of the file the probe writes them to.</param>
        /// <param name="execute">The way the probe application is driven.</param>
        /// <returns>The counts the probe reported when it exited.</returns>
        private static string ReadDisposalReport(string project, string reportFileName, Action execute)
        {
            // The probe projects are referenced with ReferenceOutputAssembly="false", so the names are repeated here
            // rather than taken from the ReportFileName constants of the probes themselves.
            var report = Path.Combine(Path.GetDirectoryName(GetProbeApplication(project))!, reportFileName);

            File.Delete(report);
            execute();

            Assert.True(File.Exists(report), $"The probe application did not write '{report}'.");

            return File.ReadAllText(report);
        }

        private IReadOnlyList<DiscoveredTest> Discover(string project, params string[] arguments)
        {
            var (exitCode, standardOutput) = Execute(project, ["--list-tests", "json", .. arguments]);

            Assert.Equal(0, exitCode);

            using var document = JsonDocument.Parse(standardOutput);

            return document.RootElement.GetProperty("tests")
                .EnumerateArray()
                .Select(test => new DiscoveredTest(
                    test.GetProperty("uid").GetString()!,
                    test.GetProperty("displayName").GetString()!,
                    test.GetProperty("type").GetProperty("typeName").GetString()!))
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

        private sealed record DiscoveredTest(string Uid, string DisplayName, string TypeName);

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
