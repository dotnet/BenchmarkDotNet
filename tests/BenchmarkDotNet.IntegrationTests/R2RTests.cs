using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.R2R;
using System.Reflection;
using System.Xml.Linq;

namespace BenchmarkDotNet.IntegrationTests
{
    public class R2RTests : BenchmarkTestExecutor
    {
        public R2RTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("R2R requires .NET Core runtime", EnvRequirement.DotNetCoreOnly)]
        public void R2RToolchainGathersReferencesBeforePublishing()
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithToolchain(CsProjR2RToolchain.R2R10_0)
                    // Explicitly selecting Release bypasses the integration-test no-dependencies shortcut.
                    .WithCustomBuildConfiguration(InfrastructureMode.ReleaseConfigurationName))
                .WithBuildTimeout(TimeSpan.FromSeconds(540))
                .WithOption(ConfigOptions.KeepBenchmarkFiles, true)
                .WithOption(ConfigOptions.LogBuildOutput, true);
            var summary = CanExecute<R2RBenchmark>(config, fullValidation: false);
            string log = logger.GetLog();
            Assert.True(summary.Reports.Length == 1, log);
            var report = summary.Reports[0];
            var paths = report.BuildResult.ArtifactsPaths;
            Output.WriteLine($"Reference-gathering artifacts: {paths.BuildArtifactsDirectoryPath}");

            string referencesList = Path.Combine(paths.BuildArtifactsDirectoryPath, "ref.txt");
            string referencesProps = Path.Combine(paths.BuildArtifactsDirectoryPath, "ref.props");
            Assert.True(File.Exists(referencesList) && new FileInfo(referencesList).Length > 0,
                $"Reference gathering did not produce a nonempty ref.txt.{Environment.NewLine}{log}");
            Assert.True(File.Exists(referencesProps),
                $"Reference gathering did not complete successfully (ref.props missing).{Environment.NewLine}{log}");

            var gatheredPaths = File.ReadAllLines(referencesList).Select(line => line.Split('|')[0]).ToArray();
            var references = XDocument.Load(referencesProps).Descendants("Reference").ToArray();
            foreach (string assemblyName in new[] { typeof(Job).Assembly.GetName().Name!, typeof(R2RBenchmark).Assembly.GetName().Name! })
            {
                var matchingReferences = references.Where(reference => (string?)reference.Attribute("Include") == assemblyName).ToArray();
                Assert.True(matchingReferences.Length == 1, $"Expected gathered reference {assemblyName}.{Environment.NewLine}{log}");
                string? hintPath = (string?)matchingReferences[0].Element("HintPath");
                Assert.True(hintPath is not null && File.Exists(hintPath), $"Gathered reference {assemblyName} is missing.{Environment.NewLine}{log}");
                Assert.True(AssemblyName.GetAssemblyName(hintPath!).Name == assemblyName, log);
                Assert.True(gatheredPaths.Any(path => string.Equals(path, hintPath, StringComparison.OrdinalIgnoreCase)), log);
            }

            Assert.True(log.Contains("BenchmarkDotNetGatherReferences=true"), log);
            Assert.False(log.Contains("--no-dependencies"), log);
            Assert.False(log.Contains("Failed to gather assembly references"), log);
            Assert.True(report.BuildResult.IsBuildSuccess, log);
            Assert.True(File.Exists(paths.ExecutablePath), log);
            string compositeAssembly = Path.ChangeExtension(paths.ExecutablePath, ".r2r.dll");
            Assert.True(File.Exists(compositeAssembly) && new FileInfo(compositeAssembly).Length > 0, log);
            Assert.True(report.ExecuteResults.Count > 0 && report.ExecuteResults.All(result => result.IsSuccess), log);
            Assert.True(report.Success, log);

            var project = XDocument.Load(paths.ProjectFilePath);
            var r2rProperties = Assert.Single(project.Root!.Elements("PropertyGroup"),
                group => group.Element("PublishReadyToRunComposite") is not null);
            Assert.Equal("true", (string?)r2rProperties.Element("SelfContained"));
            Assert.Equal("true", (string?)r2rProperties.Element("PublishReadyToRun"));
            Assert.Equal("true", (string?)r2rProperties.Element("PublishReadyToRunComposite"));
            Assert.Equal(BenchmarkDotNet.Portability.RuntimeInformation.GetPortableRuntimeIdentifier(),
                (string?)r2rProperties.Element("RuntimeIdentifier"));
        }

        [FactEnvSpecific("R2R requires .NET Core runtime", EnvRequirement.DotNetCoreOnly, EnvRequirement.NonGitHubDraftPR)]
        public void R2RToolchainCanExecuteBenchmarks()
        {
            try
            {
                var config = ManualConfig.CreateEmpty()
                    .AddJob(Job.Dry
                        .WithToolchain(CsProjR2RToolchain.R2R10_0))
                    .WithBuildTimeout(TimeSpan.FromSeconds(540));
                var summary = CanExecute<R2RBenchmark>(config);

                Assert.True(summary.Reports.Length > 0, "Expected at least one benchmark report");
                Assert.True(summary.Reports[0].Success, "Benchmark should have executed successfully");
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                    Output.WriteLine(e.SkipMessage);
                else
                    throw;
            }
        }
    }

    public class R2RBenchmark
    {
        [Benchmark]
        public void SimpleMethod()
        {
            // I don't believe there is a simple way to verify at runtime that we are actually running under r2r.
            // Reading PE format for app assemblies doesn't seem worth it.
        }
    }
}
