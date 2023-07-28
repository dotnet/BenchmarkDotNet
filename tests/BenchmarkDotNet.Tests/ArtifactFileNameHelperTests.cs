using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using System.Buffers.Tests;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ArtifactFileNameHelperTests
    {
        [FactEnvSpecific("ETW Sessions can be created only on Windows", EnvRequirement.WindowsOnly)]
        public void OnWindowsWeMustAlwaysUseOldLongPathsLimitForSessionFiles()
        {
            var config = DefaultConfig.Instance
                .WithArtifactsPath(@"C:\Projects\performance\artifacts\bin\MicroBenchmarks\Release\netcoreapp5.0\BenchmarkDotNet.Artifacts");

            var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(RentReturnArrayPoolTests<byte>), config).BenchmarksCases.First();

            var parameters = new DiagnoserActionParameters(
                process: null,
                benchmarkCase: benchmarkCase,
                new BenchmarkId(0, benchmarkCase));

            foreach (string fileExtension in new[] { "etl", "kernel.etl", "userheap.etl" })
            {
                var traceFilePath = ArtifactFileNameHelper.GetTraceFilePath(parameters, new System.DateTime(2020, 10, 1), fileExtension);

                Assert.InRange(actual: traceFilePath.Length, low: 0, high: 260);
            }
        }
    }
}

namespace System.Buffers.Tests
{
    [GenericTypeArguments(typeof(byte))] // value type
    [GenericTypeArguments(typeof(object))] // reference type
    public class RentReturnArrayPoolTests<T>
    {
        private readonly ArrayPool<T> _createdPool = ArrayPool<T>.Create();
        private const int Iterations = 100_000;
        [Params(4096)]
        public int RentalSize;

        [Params(false, true)]
        public bool ManipulateArray { get; set; }

        [Params(false, true)]
        public bool Async { get; set; }

        [Params(false, true)]
        public bool UseSharedPool { get; set; }

        private ArrayPool<T> Pool => UseSharedPool ? ArrayPool<T>.Shared : _createdPool;

        private static void Clear(T[] arr) => arr.AsSpan().Clear();

        private static T IterateAll(T[] arr)
        {
            T ret = default;
            foreach (T item in arr)
            {
                ret = item;
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task SingleSerial()
        {
            ArrayPool<T> pool = Pool;
            for (int i = 0; i < Iterations; i++)
            {
                T[] arr = pool.Rent(RentalSize);
                if (ManipulateArray) Clear(arr);
                if (Async) await Task.Yield();
                if (ManipulateArray) IterateAll(arr);
                pool.Return(arr);
            }
        }
    }
}
