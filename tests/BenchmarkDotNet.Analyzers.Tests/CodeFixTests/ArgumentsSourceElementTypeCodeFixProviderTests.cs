using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using BenchmarkDotNet.CodeFixers;

namespace BenchmarkDotNet.Analyzers.Tests.CodeFixTests;

public class ArgumentsSourceValueTupleCodeFixProviderTests : CodeFixTestFixture<ArgumentsAttributeAnalyzer, ArgumentsSourceElementTypeCodeFixProvider>
{
    public ArgumentsSourceValueTupleCodeFixProviderTests() : base(ArgumentsAttributeAnalyzer.ShouldYieldValueTupleRule) { }

    /// <summary>
    /// The benchmark and the diagnostic are the same in every case here; only the source's rows differ.
    /// A null <paramref name="fixedValues"/> says no fix is offered, which is asserted as the unchanged code
    /// still carrying the diagnostic. It has to be spelled out: the harness verifies the fixed state only when
    /// FixedCode is set, so an unset one asserts nothing at all.
    /// </summary>
    private void Source(string values, string? fixedValues)
    {
        TestCode = Code(values, reported: true);
        FixedCode = fixedValues == null ? Code(values, reported: true) : Code(fixedValues, reported: false);

        AddExpectedDiagnostic(0, "Values", "(int, string)");
    }

    private static string Code(string values, bool reported)
        => /* lang=c#-test */ $$"""
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
            {{values}}
                [Benchmark]
                [ArgumentsSource({{(reported ? "{|#0:nameof(Values)|}" : "nameof(Values)")}})]
                public void Run(int number, string text) { }
            }
            """.ReplaceLineEndings();

    // Both halves are rewritten: the declaration and every value it hands back. Retyping one without the other
    // would leave the source not compiling, which is why the fix is only offered when all of them can be.
    [Fact]
    public async Task CodeFix_retypes_an_object_array_source_to_a_tuple()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { 1, "one" };
                    yield return new object[] { 2, "two" };
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return (1, "one");
                    yield return (2, "two");
                }

            """);

        await RunAsync();
    }

    // Every syntax that builds a row has to be read, not only the explicitly typed array: a collection
    // expression is what a source written against a recent language version most likely says.
    [Fact]
    public async Task CodeFix_rewrites_a_collection_expression()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return [1, "one"];
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return (1, "one");
                }

            """);

        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_rewrites_a_sized_array()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[2] { 1, "one" };
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return (1, "one");
                }

            """);

        await RunAsync();
    }

    // An implicitly typed array needs a cast on one element for the array to be an object[] at all. That cast
    // belongs to the shape being replaced, so carrying it into the tuple would say object where the parameter
    // takes int - it goes, rather than the fix emitting code that does not compile.
    [Fact]
    public async Task CodeFix_drops_a_cast_that_only_served_the_array()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new[] { (object)1, "one" };
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return (1, "one");
                }

            """);

        await RunAsync();
    }

    // A yield break hands back no row, so it is not one to rewrite - and must not stop the rows that are.
    [Fact]
    public async Task CodeFix_rewrites_a_source_that_also_yield_breaks()
    {
        Source(
            """
                public static bool Skip = false;

                public static IEnumerable<object[]> Values()
                {
                    if (Skip) yield break;
                    yield return new object[] { 1, "one" };
                }

            """,
            """
                public static bool Skip = false;

                public static IEnumerable<(int, string)> Values()
                {
                    if (Skip) yield break;
                    yield return (1, "one");
                }

            """);

        await RunAsync();
    }

    // How many arguments a spread stands for is not written down, so the row cannot become a tuple of any
    // particular arity. Reported, not fixed.
    [Fact]
    public async Task CodeFix_is_not_offered_for_a_spread_element()
    {
        Source(
            """
                public static object[] Parts = null;

                public static IEnumerable<object[]> Values()
                {
                    yield return [.. Parts];
                }

            """,
            null);

        await RunAsync();
    }

    // A source that returns its rows rather than yielding them keeps its declared enumerable where that is
    // already the one it is read as.
    [Fact]
    public async Task CodeFix_retypes_a_source_that_returns_its_rows()
    {
        Source(
            """
                public static IEnumerable<object[]> Values() => new[] { new object[] { 1, "one" } };

            """,
            """
                public static IEnumerable<(int, string)> Values() => new[] { (1, "one") };

            """);

        await RunAsync();
    }

    // Only the element type is rewritten, never the enclosing type name: here the file has no using for the
    // namespace, so rebuilding the declaration as an unqualified IEnumerable<> would not compile.
    [Fact]
    public async Task CodeFix_keeps_a_qualified_return_type_qualified()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static System.Collections.Generic.IEnumerable<object[]> Values()
                {
                    yield return new object[] { 1, "one" };
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(int number, string text) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static System.Collections.Generic.IEnumerable<(int, string)> Values()
                {
                    yield return (1, "one");
                }

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(int number, string text) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "(int, string)");
        await RunAsync();
    }

    // A source may be anything that implements the enumerable. Where it is not an iterator its rows are one
    // collection, and that collection's own type cannot survive its element type changing - a List<object[]>
    // does not become a List of tuples by rewriting the rows - so the declaration becomes the enumerable
    // BenchmarkDotNet reads it as either way.
    [Fact]
    public async Task CodeFix_retypes_a_list_source_to_the_enumerable()
    {
        Source(
            """
                public static List<object[]> Values() => [[1, "one"], [2, "two"]];

            """,
            """
                public static IEnumerable<(int, string)> Values() => [(1, "one"), (2, "two")];

            """);

        await RunAsync();
    }

    // An array source names no namespace, so the enumerable it becomes is written out in full rather than
    // assuming a using the file need not have.
    [Fact]
    public async Task CodeFix_retypes_an_array_source_to_the_enumerable()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static object[][] Values() => new[] { new object[] { 1, "one" } };

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(int number, string text) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static global::System.Collections.Generic.IEnumerable<(int, string)> Values() => new[] { (1, "one") };

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(int number, string text) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "(int, string)");
        await RunAsync();
    }

    // A collection initializer reads the same as the collection expression that replaced it.
    [Fact]
    public async Task CodeFix_retypes_a_source_built_with_a_collection_initializer()
    {
        Source(
            """
                public static List<object[]> Values() => new List<object[]> { new object[] { 1, "one" } };

            """,
            """
                public static IEnumerable<(int, string)> Values() => new[] { (1, "one") };

            """);

        await RunAsync();
    }

    // The same refusal applies to the collection holding the rows as to a row: a spread does not say how many
    // rows it stands for, so there is no telling which of them would have to be rewritten.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_rows_come_from_a_spread()
    {
        Source(
            """
                public static List<object[]> Other = null;

                public static List<object[]> Values() => [.. Other];

            """,
            null);

        await RunAsync();
    }

    // A source whose rows are not written down here - handed back from somewhere else - has nothing to rewrite,
    // so retyping the declaration alone would leave it not compiling.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_rows_are_not_written_here()
    {
        Source(
            """
                public static List<object[]> Other = null;

                public static List<object[]> Values() => Other;

            """,
            null);

        await RunAsync();
    }

    // Only the syntax around the values is rewritten, so a row written across several lines stays that way.
    [Fact]
    public async Task CodeFix_keeps_the_layout_of_a_row_written_over_several_lines()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[]
                    {
                        1,
                        "one"
                    };
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return
                    (
                        1,
                        "one"
                    );
                }

            """);

        await RunAsync();
    }

    // The values and the separators come through verbatim, so whatever was written against them comes with
    // them - a fix that quietly deleted a comment is not one a review would catch.
    [Fact]
    public async Task CodeFix_keeps_the_comments_written_against_a_row()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[]
                    {
                        1, // the number
                        "one" // the text
                    };
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return
                    (
                        1, // the number
                        "one" // the text
                    );
                }

            """);

        await RunAsync();
    }

    // A comment on the syntax that goes - the array's own - moves onto the parenthesis that replaces the brace,
    // so removing `new object[]` does not remove what was written about it.
    [Fact]
    public async Task CodeFix_keeps_a_comment_written_on_the_syntax_it_removes()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] /* a row */
                    {
                        1,
                        "one"
                    };
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return /* a row */
                    (
                        1,
                        "one"
                    );
                }

            """);

        await RunAsync();
    }

    // A collection expression opens with the bracket itself, so the parenthesis simply replaces it - there is no
    // `new object[]` in front of it to account for, and nothing to carry from one.
    [Fact]
    public async Task CodeFix_keeps_the_layout_of_a_collection_expression_row()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return
                    [
                        1,
                        "one"
                    ];
                }

            """,
            """
                public static IEnumerable<(int, string)> Values()
                {
                    yield return
                    (
                        1,
                        "one"
                    );
                }

            """);

        await RunAsync();
    }

    // Both levels at once: the rows keep their own lines inside the collection, and each becomes a tuple in place.
    [Fact]
    public async Task CodeFix_keeps_the_layout_of_a_collection_of_rows()
    {
        Source(
            """
                public static List<object[]> Values() =>
                [
                    [1, "one"],
                    [2, "two"],
                ];

            """,
            """
                public static IEnumerable<(int, string)> Values() =>
                [
                    (1, "one"),
                    (2, "two"),
                ];

            """);

        await RunAsync();
    }

    // A row that is built up over several statements is not written down as a row anywhere, so there is nothing
    // here to turn into a tuple - only the assignments that would each have to be understood.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_row_is_filled_element_by_element()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    var row = new object[2];
                    row[0] = 1;
                    row[1] = "one";
                    yield return row;
                }

            """,
            null);

        await RunAsync();
    }

    // Nor where the row is written into a local first: what the yield hands back is the local, and retyping the
    // declaration around it would leave the local naming the shape it no longer holds.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_row_is_yielded_through_a_local()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    var row = new object[] { 1, "one" };
                    yield return row;
                }

            """,
            null);

        await RunAsync();
    }
}

public class ArgumentsSourceParameterTypeCodeFixProviderTests : CodeFixTestFixture<ArgumentsAttributeAnalyzer, ArgumentsSourceElementTypeCodeFixProvider>
{
    public ArgumentsSourceParameterTypeCodeFixProviderTests() : base(ArgumentsAttributeAnalyzer.ShouldYieldParameterTypeRule) { }

    // A single argument is fed the value itself, so only the declaration changes - the values already are what the
    // parameter takes.
    [Fact]
    public async Task CodeFix_retypes_an_object_source_to_the_parameter_type()
    {
        TestCode = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<object> Values()
                {
                    yield return 1;
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<int> Values()
                {
                    yield return 1;
                }

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "int");
        await RunAsync();
    }

    // The cast is what made the value an object; once the source names the parameter's type it is what stops the
    // source compiling, so it goes with the declaration it served.
    [Fact]
    public async Task CodeFix_drops_a_cast_that_only_served_the_declaration()
    {
        TestCode = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<object> Values()
                {
                    yield return (object)1;
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<int> Values()
                {
                    yield return 1;
                }

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "int");
        await RunAsync();
    }

    // The suggestion is only that the source could name the parameter's type. Where a value it hands back is not
    // one, naming it would be a compiler error, so the fix is withheld and the source left to its author.
    [Fact]
    public async Task CodeFix_is_not_offered_where_a_value_is_not_the_parameter_type()
    {
        // The same code either side: the fix must not fire, and the diagnostic stays.
        var code = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<object> Values()
                {
                    yield return "text";
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        TestCode = code;
        FixedCode = code;

        AddExpectedDiagnostic(0, "Values", "int");
        await RunAsync();
    }

    // The diagnostic names the parameter's type in full so its message reads clearly. The fix writes it the way
    // it binds where the declaration is, which is the way someone would have written it there.
    [Fact]
    public async Task CodeFix_writes_the_parameter_type_as_it_binds_at_the_declaration()
    {
        TestCode = /* lang=c#-test */ """
            using System;
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<object> Values()
                {
                    yield return TimeSpan.Zero;
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(TimeSpan time) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using System;
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static IEnumerable<TimeSpan> Values()
                {
                    yield return TimeSpan.Zero;
                }

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(TimeSpan time) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "System.TimeSpan");
        await RunAsync();
    }

    // An async source keeps the enumerable it names: rewriting IAsyncEnumerable to IEnumerable would change how
    // the values are read, which is not what was reported.
    [Fact]
    public async Task CodeFix_keeps_an_async_source_asynchronous()
    {
        TestCode = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static async IAsyncEnumerable<object> Values()
                {
                    yield return 1;
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public static async IAsyncEnumerable<int> Values()
                {
                    yield return 1;
                }

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "int");
        await RunAsync();
    }
}

public class ArgumentsSourceUnwrapCodeFixProviderTests : CodeFixTestFixture<ArgumentsAttributeAnalyzer, ArgumentsSourceElementTypeCodeFixProvider>
{
    public ArgumentsSourceUnwrapCodeFixProviderTests() : base(ArgumentsAttributeAnalyzer.MustYieldWhatParametersTakeRule) { }

    private void Source(string values, string? fixedValues)
    {
        TestCode = Code(values, reported: true);
        FixedCode = fixedValues == null ? Code(values, reported: true) : Code(fixedValues, reported: false);

        AddExpectedDiagnostic(0, "Values", "object[]", "int", "");
    }

    private static string Code(string values, bool reported)
        => /* lang=c#-test */ $$"""
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
            {{values}}
                [Benchmark]
                [ArgumentsSource({{(reported ? "{|#0:nameof(Values)|}" : "nameof(Values)")}})]
                public void Run(int number) { }
            }
            """.ReplaceLineEndings();

    // The break: a single argument is fed the value itself, so the array that used to wrap it goes, and the
    // source names what the parameter takes. This is the migration the documentation describes.
    [Fact]
    public async Task CodeFix_unwraps_a_single_argument_row()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { 1 };
                    yield return new object[] { 2 };
                }

            """,
            """
                public static IEnumerable<int> Values()
                {
                    yield return 1;
                    yield return 2;
                }

            """);

        await RunAsync();
    }

    // The same unwrapping wherever the rows are written, so a source that is not an iterator moves too.
    [Fact]
    public async Task CodeFix_unwraps_the_rows_of_a_collection()
    {
        Source(
            """
                public static List<object[]> Values() => [[1], [2]];

            """,
            """
                public static IEnumerable<int> Values() => [1, 2];

            """);

        await RunAsync();
    }

    // A row holding more than one value is not a wrapped argument - the benchmark takes one - so there is nothing
    // to unwrap and the declaration is left to its author.
    [Fact]
    public async Task CodeFix_is_not_offered_where_a_row_holds_more_than_one_value()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { 1, 2 };
                }

            """,
            null);

        await RunAsync();
    }

    // Nor where the value it holds is not what the parameter takes. Only a by-ref-like parameter is reached
    // through a conversion; every other one names what it takes, so nothing but that type will do.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_wrapped_value_is_not_the_parameter_type()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { "text" };
                }

            """,
            null);

        await RunAsync();
    }
}

public class ArgumentsSourceByRefLikeUnwrapCodeFixProviderTests : CodeFixTestFixture<ArgumentsAttributeAnalyzer, ArgumentsSourceElementTypeCodeFixProvider>
{
    public ArgumentsSourceByRefLikeUnwrapCodeFixProviderTests() : base(ArgumentsAttributeAnalyzer.MustYieldWhatParametersTakeRule) { }

    private void Source(string values, string? fixedValues)
    {
        TestCode = Code(values, reported: true);
        FixedCode = fixedValues == null ? Code(values, reported: true) : Code(fixedValues, reported: false);

        AddExpectedDiagnostic(0, "Values", "object[]", "System.ReadOnlySpan<byte>", "");
    }

    private static string Code(string values, bool reported)
        => /* lang=c#-test */ $$"""
            using System;
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
            {{values}}
                [Benchmark]
                [ArgumentsSource({{(reported ? "{|#0:nameof(Values)|}" : "nameof(Values)")}})]
                public void Run(ReadOnlySpan<byte> bytes) { }
            }
            """.ReplaceLineEndings();

    // No source can yield a ref struct, so the diagnostic has no type to name - but the rows do. What they hold
    // is what the source should be declared to yield, provided the parameter can be reached from it.
    [Fact]
    public async Task CodeFix_unwraps_a_row_feeding_a_by_ref_like_parameter()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { new byte[] { 1, 2, 3 } };
                }

            """,
            """
                public static IEnumerable<byte[]> Values()
                {
                    yield return new byte[] { 1, 2, 3 };
                }

            """);

        await RunAsync();
    }

    // BenchmarkDotNet writes the cast for a by-ref-like parameter, so an explicit operator reaches it as well as
    // an implicit one - the source only has to yield the type that operator converts from.
    [Fact]
    public async Task CodeFix_unwraps_a_row_reaching_the_parameter_by_an_explicit_operator()
    {
        TestCode = /* lang=c#-test */ """
            using System;
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public class Convertible
                {
                    public static explicit operator ReadOnlySpan<byte>(Convertible convertible) => default;
                }

                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { new Convertible() };
                }

                [Benchmark]
                [ArgumentsSource({|#0:nameof(Values)|})]
                public void Run(ReadOnlySpan<byte> bytes) { }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using System;
            using System.Collections.Generic;
            using BenchmarkDotNet.Attributes;

            public class BenchmarkClass
            {
                public class Convertible
                {
                    public static explicit operator ReadOnlySpan<byte>(Convertible convertible) => default;
                }

                public static IEnumerable<Convertible> Values()
                {
                    yield return new Convertible();
                }

                [Benchmark]
                [ArgumentsSource(nameof(Values))]
                public void Run(ReadOnlySpan<byte> bytes) { }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0, "Values", "object[]", "System.ReadOnlySpan<byte>", "");
        await RunAsync();
    }

    // The rows have to agree on one type, even where each of them reaches the parameter on its own: a source
    // yields one element type, and declaring either of these would leave the other row not compiling.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_rows_hold_different_types()
    {
        Source(
            """
                public class Convertible
                {
                    public static implicit operator ReadOnlySpan<byte>(Convertible convertible) => default;
                }

                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { new byte[] { 1, 2, 3 } };
                    yield return new object[] { new Convertible() };
                }

            """,
            null);

        await RunAsync();
    }

    // And the type they agree on has to be one the parameter is actually reachable from.
    [Fact]
    public async Task CodeFix_is_not_offered_where_the_parameter_cannot_be_reached_from_what_the_rows_hold()
    {
        Source(
            """
                public static IEnumerable<object[]> Values()
                {
                    yield return new object[] { "text" };
                }

            """,
            null);

        await RunAsync();
    }
}
