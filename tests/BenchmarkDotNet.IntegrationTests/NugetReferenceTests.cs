using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NugetReferenceTests : BenchmarkTestExecutor
    {
        public NugetReferenceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void UserCanSpecifyCustomNuGetPackageDependency()
        {
            var toolchain = RuntimeInformation.IsFullFramework
                ? CsProjClassicNetToolchain.Current.Value // this .NET toolchain will do the right thing, the default RoslynToolchain does not support it
                : CsProjCoreToolchain.Current.Value;

            var job = Job.Dry.With(toolchain).WithNuget("Newtonsoft.Json", "11.0.2");
            var config = CreateSimpleConfig(job: job);

            CanExecute<WithCallToNewtonsoft>(config);
        }

        public class WithCallToNewtonsoft
        {
            [Benchmark] public void SerializeAnonymousObject() => JsonConvert.SerializeObject(new { hello = "world", price = 1.99, now = DateTime.UtcNow });
        }
    }
}
