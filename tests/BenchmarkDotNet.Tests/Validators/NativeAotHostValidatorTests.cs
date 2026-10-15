using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Validators;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Validators
{
    public class NativeAotHostValidatorTests
    {
        private static readonly IToolchain NoEmit = InProcessNoEmitToolchain.Default;

        private static async Task<ValidationError[]> Validate<T>(IToolchain? toolchain, bool isNativeAotHost = true)
        {
            var job = toolchain is null ? Job.Dry : Job.Dry.WithToolchain(toolchain);
            var config = ManualConfig.CreateEmpty().AddJob(job);
            return await new NativeAotHostValidator(isNativeAotHost)
                .ValidateAsync(BenchmarkConverter.TypeToBenchmarks(typeof(T), config))
                .ToArrayAsync();
        }

        [Fact]
        public async Task NotNativeAotHost_NoErrors()
        {
            Assert.Empty(await Validate<ReturnsInt>(toolchain: null, isNativeAotHost: false));
            Assert.Empty(await Validate<ReturnsInt>(InProcessEmitToolchain.Default, isNativeAotHost: false));
            Assert.Empty(await Validate<ReturnsInt>(NoEmit, isNativeAotHost: false));
        }

        [Fact]
        public async Task OutOfProcessToolchain_Error()
        {
            var error = Assert.Single(await Validate<ReturnsVoid>(toolchain: null));
            Assert.True(error.IsCritical);
            Assert.Contains("cannot generate and build a project", error.Message);
            Assert.Contains("--runtimes nativeaot", error.Message);
        }

        [Fact]
        public async Task InProcessEmitToolchain_Error()
        {
            var error = Assert.Single(await Validate<ReturnsVoid>(InProcessEmitToolchain.Default));
            Assert.True(error.IsCritical);
            Assert.Contains(nameof(InProcessEmitToolchain), error.Message);
            Assert.Contains(nameof(InProcessNoEmitToolchain), error.Message);
        }

        [Fact]
        public async Task NoEmit_NonGenericReturnTypes_NoErrors()
        {
            Assert.Empty(await Validate<ReturnsVoid>(NoEmit));
            Assert.Empty(await Validate<ReturnsTaskOrValueTask>(NoEmit));
        }

        [Fact]
        public async Task NoEmit_ValueReturned_ErrorPerBenchmark()
        {
            var errors = await Validate<ReturnsValues>(NoEmit);
            Assert.Equal(4, errors.Length);
            Assert.All(errors, error => Assert.True(error.IsCritical));
            Assert.Contains(errors, error => error.Message.Contains("'Int' because it returns Int32"));
            Assert.Contains(errors, error => error.Message.Contains("'String' because it returns String"));
            Assert.Contains(errors, error => error.Message.Contains("'TaskOfInt' because it returns Task<Int32>"));
            Assert.Contains(errors, error => error.Message.Contains("'RefInt' because it returns Int32&"));
        }

        [Fact]
        public async Task NoEmit_SetupReturningValue_Error()
        {
            var error = Assert.Single(await Validate<SetupReturnsValueTaskOfInt>(NoEmit));
            Assert.Contains("'Setup' because it returns ValueTask<Int32>", error.Message);
        }

        [Fact]
        public async Task NoEmit_CustomActionFactory_NoErrors()
        {
            var toolchain = InProcessNoEmitToolchain.From(new InProcessNoEmitSettings { BenchmarkActionFactory = new NoActionFactory() });
            Assert.Empty(await Validate<ReturnsValues>(toolchain));
        }

        public class ReturnsVoid
        {
            [Benchmark] public void Void() { }
        }

        public class ReturnsInt
        {
            [Benchmark] public int Int() => 1;
        }

        public class ReturnsTaskOrValueTask
        {
            [Benchmark] public Task Task() => System.Threading.Tasks.Task.CompletedTask;
            [Benchmark] public ValueTask ValueTask() => default;
        }

        public class ReturnsValues
        {
            private int field;

            [Benchmark] public int Int() => 1;
            [Benchmark] public string String() => "";
            [Benchmark] public Task<int> TaskOfInt() => Task.FromResult(1);
            [Benchmark] public ref int RefInt() => ref field;
        }

        public class SetupReturnsValueTaskOfInt
        {
            [GlobalSetup] public ValueTask<int> Setup() => new(1);
            [Benchmark] public void Void() { }
        }

        private class NoActionFactory : IBenchmarkActionFactory
        {
            public bool TryCreate(object instance, MethodInfo targetMethod, int unrollFactor, [NotNullWhen(true)] out IBenchmarkAction? benchmarkAction)
            {
                benchmarkAction = null;
                return false;
            }
        }
    }
}
