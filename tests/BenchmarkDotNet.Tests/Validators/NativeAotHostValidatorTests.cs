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
            Assert.Empty(await Validate<ReturnsUnsupportedValues>(toolchain: null, isNativeAotHost: false));
            Assert.Empty(await Validate<ReturnsUnsupportedValues>(InProcessEmitToolchain.Default, isNativeAotHost: false));
            Assert.Empty(await Validate<ReturnsUnsupportedValues>(NoEmit, isNativeAotHost: false));
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
        public async Task NoEmit_ReturnTypesWithClosedActions_NoErrors()
        {
            Assert.Empty(await Validate<ReturnsSupportedValues>(NoEmit));
        }

        [Fact]
        public async Task NoEmit_ReturnTypesWithoutClosedActions_ErrorPerBenchmark()
        {
            var errors = await Validate<ReturnsUnsupportedValues>(NoEmit);
            Assert.Equal(3, errors.Length);
            Assert.All(errors, error => Assert.True(error.IsCritical));
            Assert.Contains(errors, error => error.Message.Contains("'Struct' because it returns CustomStruct"));
            Assert.Contains(errors, error => error.Message.Contains("'ValueTaskOfString' because it returns ValueTask<String>"));
            Assert.Contains(errors, error => error.Message.Contains("'RefStruct' because it returns CustomStruct&"));
        }

        [Fact]
        public async Task NoEmit_SetupReturningUnsupportedType_Error()
        {
            var error = Assert.Single(await Validate<SetupReturnsValueTaskOfString>(NoEmit));
            Assert.Contains("'Setup' because it returns ValueTask<String>", error.Message);
        }

        [Fact]
        public async Task NoEmit_CustomActionFactory_NoErrors()
        {
            var toolchain = InProcessNoEmitToolchain.From(new InProcessNoEmitSettings { BenchmarkActionFactory = new NoActionFactory() });
            Assert.Empty(await Validate<ReturnsUnsupportedValues>(toolchain));
        }

        public class ReturnsVoid
        {
            [Benchmark] public void Void() { }
        }

        public class ReturnsTaskOrValueTask
        {
            [Benchmark] public Task Task() => System.Threading.Tasks.Task.CompletedTask;
            [Benchmark] public ValueTask ValueTask() => default;
        }

        public class ReturnsSupportedValues
        {
            [Benchmark] public int Int() => 1;
            [Benchmark] public double Double() => 1;
            [Benchmark] public string String() => "";
            [Benchmark] public int[] Array() => [];
            [Benchmark] public Task<int> TaskOfInt() => Task.FromResult(1);
            [Benchmark] public Task<CustomStruct> TaskOfStruct() => Task.FromResult(default(CustomStruct));
            [Benchmark] public ValueTask<long> ValueTaskOfLong() => new(1);
            [Benchmark] public IAsyncEnumerable<int> AsyncEnumerableOfInt() => AsyncEnumerable.Empty<int>();
            [Benchmark] public IAsyncEnumerable<string> AsyncEnumerableOfString() => AsyncEnumerable.Empty<string>();
            [Benchmark] public ref int RefInt() => ref field;

            private int field;
        }

        public class ReturnsUnsupportedValues
        {
            private CustomStruct field;

            [Benchmark] public CustomStruct Struct() => default;
            [Benchmark] public ValueTask<string> ValueTaskOfString() => new("");
            [Benchmark] public ref CustomStruct RefStruct() => ref field;
        }

        public class SetupReturnsValueTaskOfString
        {
            [GlobalSetup] public ValueTask<string> Setup() => new("");
            [Benchmark] public void Void() { }
        }

        public struct CustomStruct
        {
            public long Value;
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
