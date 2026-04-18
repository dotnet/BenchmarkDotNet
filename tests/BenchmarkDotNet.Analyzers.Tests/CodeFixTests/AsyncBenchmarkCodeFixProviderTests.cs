using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using BenchmarkDotNet.CodeFixers;

namespace BenchmarkDotNet.Analyzers.Tests.CodeFixTests;

public class AsyncBenchmarkCodeFixProviderTests : CodeFixTestFixture<AsyncBenchmarkAnalyzer, AsyncBenchmarkCodeFixProvider>
{
    public AsyncBenchmarkCodeFixProviderTests() : base(AsyncBenchmarkAnalyzer.AsyncBenchmarkShouldHaveCancellationTokenRule) { }

    [Fact]
    public async Task CodeFix_AddsPropertyAndUsings_WhenBothAreMissing()
    {
        var testCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [Benchmark]
                public async Task {|#0:AsyncBenchmark|}()
                {
                    await Task.Delay(100);
                }
            }
            """.ReplaceLineEndings();

        var fixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [BenchmarkCancellation]
                public CancellationToken CancellationToken { get; set; }

                [Benchmark]
                public async Task AsyncBenchmark()
                {
                    await Task.Delay(100);
                }
            }
            """.ReplaceLineEndings();

        TestCode = testCode;
        FixedCode = fixedCode;
        AddExpectedDiagnostic(0, "BenchmarkClass");

        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_AddsPropertyOnly_WhenUsingsExist()
    {
        var testCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [Benchmark]
                public async Task {|#0:AsyncBenchmark|}()
                {
                    await Task.Delay(100);
                }
            }
            """.ReplaceLineEndings();

        var fixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [BenchmarkCancellation]
                public CancellationToken CancellationToken { get; set; }

                [Benchmark]
                public async Task AsyncBenchmark()
                {
                    await Task.Delay(100);
                }
            }
            """.ReplaceLineEndings();

        TestCode = testCode;
        FixedCode = fixedCode;
        AddExpectedDiagnostic(0, "BenchmarkClass");

        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_InsertsPropertyAfterExistingProperties()
    {
        var testCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [Params(10, 100)]
                public int Size { get; set; }

                [Benchmark]
                public async Task {|#0:AsyncBenchmark|}()
                {
                    await Task.Delay(Size);
                }
            }
            """.ReplaceLineEndings();

        var fixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [Params(10, 100)]
                public int Size { get; set; }

                [BenchmarkCancellation]
                public CancellationToken CancellationToken { get; set; }

                [Benchmark]
                public async Task AsyncBenchmark()
                {
                    await Task.Delay(Size);
                }
            }
            """.ReplaceLineEndings();

        TestCode = testCode;
        FixedCode = fixedCode;
        AddExpectedDiagnostic(0, "BenchmarkClass");

        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_InsertsPropertyBeforeMethods_WhenNoPropertiesExist()
    {
        var testCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [Benchmark]
                public async Task {|#0:AsyncBenchmark|}()
                {
                    await Task.Delay(100);
                }

                [Benchmark]
                public async Task AsyncBenchmark2()
                {
                    await Task.Delay(200);
                }
            }
            """.ReplaceLineEndings();

        var fixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;
            using System.Threading;
            using System.Threading.Tasks;

            public class BenchmarkClass
            {
                [BenchmarkCancellation]
                public CancellationToken CancellationToken { get; set; }

                [Benchmark]
                public async Task AsyncBenchmark()
                {
                    await Task.Delay(100);
                }

                [Benchmark]
                public async Task AsyncBenchmark2()
                {
                    await Task.Delay(200);
                }
            }
            """.ReplaceLineEndings();

        TestCode = testCode;
        FixedCode = fixedCode;
        AddExpectedDiagnostic(0, "BenchmarkClass");

        await RunAsync();
    }
}
