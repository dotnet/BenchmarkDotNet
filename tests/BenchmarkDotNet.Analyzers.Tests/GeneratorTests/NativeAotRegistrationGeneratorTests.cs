#if NET
using BenchmarkDotNet.Analyzers.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Analyzers.Tests.GeneratorTests;

public class NativeAotRegistrationGeneratorTests
{
    private const string Benchmarks = """
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using BenchmarkDotNet.Attributes;

        namespace Sample
        {
            public struct MyStruct { public long Value; }
            public enum MyEnum { A, B }

            public class Values
            {
                private MyStruct field;

                [GlobalSetup] public ValueTask<long> Setup() => new(1);
                [Benchmark] public int Int() => 1;
                [Benchmark] public string String() => "";
                [Benchmark] public void Void() { }
                [Benchmark] public MyStruct Struct() => default;
                [Benchmark] public ref MyStruct RefStruct() => ref field;
                [Benchmark] public ValueTask<string> ValueTaskOfString() => new("");
                [Benchmark] public Task<MyStruct> TaskOfStruct() => Task.FromResult(default(MyStruct));
                [Benchmark] public IAsyncEnumerable<MyEnum> AsyncEnumerableOfEnum() => null!;
                [Benchmark] public IAsyncEnumerable<string> AsyncEnumerableOfString() => null!;
                [Benchmark] public (int, long) Tuple() => default;
                [Benchmark] private Hidden HiddenStruct() => default;
                [Benchmark] public ValueTask PlainValueTask() => default;

                private struct Hidden { }
            }

            public class Derived : Values { }

            [GenericTypeArguments(typeof(int))]
            [GenericTypeArguments(typeof(MyStruct))]
            public class Generic<T>
            {
                [Benchmark] public T Default() => default!;
            }

            public abstract class Abstract
            {
                [Benchmark] public decimal Decimal() => 1;
            }

            public class WithoutDefaultConstructor
            {
                public WithoutDefaultConstructor(int value) { }
                [Benchmark] public void Run() { }
            }

            public class NotABenchmark
            {
                public System.Guid Guid() => default;
            }
        }
        """;

    [Fact]
    public void WithoutPublishAot_GeneratesNothing()
    {
        var (generated, _) = Run(Benchmarks, publishAot: false);
        Assert.Null(generated);
    }

    [Fact]
    public void WithPublishAot_KeepsBenchmarkTypesAndRegistersResultTypes()
    {
        var (generated, compilation) = Run(Benchmarks, publishAot: true);

        Assert.NotNull(generated);
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        string[] keptTypes =
        [
            "global::Sample.Values", "global::Sample.Derived", "global::Sample.Generic<int>", "global::Sample.Generic<global::Sample.MyStruct>",
        ];
        foreach (var type in keptTypes)
            Assert.Contains($"DynamicallyAccessedMemberTypes.All, typeof({type}))]", generated);
        foreach (var type in keptTypes)
            Assert.Contains($"NativeAotRegistration.RegisterBenchmarkType<{type}>();", generated);
        Assert.DoesNotContain("RegisterBenchmarkType<global::Sample.WithoutDefaultConstructor>", generated);
        Assert.Contains("typeof(global::Sample.WithoutDefaultConstructor))]", generated);
        Assert.DoesNotContain("typeof(global::Sample.Abstract)", generated);
        Assert.DoesNotContain("typeof(global::Sample.NotABenchmark)", generated);

        var registered = generated!.Split('\n')
            .Where(line => line.Contains("RegisterResultType<"))
            .Select(line => line.Trim().Replace("global::BenchmarkDotNet.Running.NativeAotRegistration.RegisterResultType<", "").Replace(">();", ""))
            .ToArray();

        // Value types (also from setup methods, inherited and generic benchmarks), ref returns, ValueTask<T> results and the items
        // of IAsyncEnumerable<T> of a value type. Reference types, Task<T>, void, ValueTask and inaccessible types need nothing.
        Assert.Equal(
            ["(int, long)", "global::Sample.MyEnum", "global::Sample.MyStruct", "int", "long", "string"],
            registered.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void WithPublishTrimmed_Generates()
    {
        var (generated, _) = Run(Benchmarks, publishAot: false, publishTrimmed: true);
        Assert.NotNull(generated);
    }

    [Fact]
    public void WithoutBenchmarks_GeneratesNothing()
    {
        var (generated, _) = Run("public class Plain { public int Value() => 1; }", publishAot: true);
        Assert.Null(generated);
    }

    private static (string? Generated, Compilation Compilation) Run(string source, bool publishAot, bool publishTrimmed = false)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(BenchmarkDotNet.Running.NativeAotRegistration).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(BenchmarkDotNet.Attributes.BenchmarkAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create("Sample",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            [new NativeAotRegistrationGenerator().AsSourceGenerator()],
            optionsProvider: new OptionsProvider(publishAot, publishTrimmed),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);
        Assert.Empty(diagnostics);

        var generated = updated.SyntaxTrees.Skip(1).SingleOrDefault()?.ToString();
        return (generated, updated);
    }

    private sealed class OptionsProvider(bool publishAot, bool publishTrimmed) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(new Dictionary<string, string>
        {
            ["build_property.PublishAot"] = publishAot ? "true" : "",
            ["build_property.PublishTrimmed"] = publishTrimmed ? "true" : "",
        });

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Options([]);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Options([]);
    }

    private sealed class Options(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }
}
#endif
