using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Reflection;

namespace BenchmarkDotNet.Parameters
{
    internal static class SmartParamBuilder
    {
        internal static IReadOnlyList<ParameterValue> CreateForParams(Type parameterType, MemberInfo source, object?[] values)
        {
            // A one-element object[] around a constant is unwrapped to the constant, which is then rendered inline
            // and needs no index. Only around a constant: a value that has to be read back from the source keeps
            // the whole array, because the generated code emits no index here to reach inside one with, and
            // unwrapping only the in-process side would have the two toolchains assign different values.
            if (values.All(value => value is object[] { Length: 1 } wrapper && SourceCodeHelper.IsCompilationTimeConstant(wrapper[0])))
                values = values.Select(value => ((object[]) value!)[0]).ToArray();

            return values.Select((value, index) =>
                SourceCodeHelper.IsCompilationTimeConstant(value)
                    ? (ParameterValue) new ParameterValue.Constant(value, parameterType)
                    : new ParameterValue.FromSource(value, new SourceRead(source, index), elementIndex: null, parameterType)).ToArray();
        }

        internal static ParameterInstances CreateForArguments(
            MethodInfo benchmark,
            ParameterDefinition[] parameterDefinitions,
            (MemberInfo source, object?[] values) valuesInfo,
            int sourceIndex,
            SummaryStyle summaryStyle)
        {
            var unwrappedValue = valuesInfo.values[sourceIndex];

            // One read for the whole row: every parameter below takes its value out of this one, so the generated
            // code enumerates the source once per case rather than once per argument.
            var read = new SourceRead(valuesInfo.source, sourceIndex);

            if (unwrappedValue is object[] array)
            {
                var firstParameterType = parameterDefinitions.FirstOrDefault()?.ParameterType;

                // An object[] for a benchmark taking a single argument: the one that takes the array itself, or a
                // by-ref-like one built from it.
                if (parameterDefinitions.Length == 1 && array.Length == 1 && firstParameterType is not null
                    && (array[0]?.GetType() == firstParameterType || firstParameterType.IsStackOnlyWithImplicitCast(array[0])))
                {
                    return new ParameterInstances(
                        [Create(parameterDefinitions, array[0], read, argumentIndex: 0, summaryStyle)]);
                }

                if (parameterDefinitions.Length > 1)
                {
                    if (parameterDefinitions.Length != array.Length)
                        throw new InvalidOperationException($"Benchmark {benchmark.Name} has invalid number of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]!" +
                            $" {array.Length} instead of {parameterDefinitions.Length}.");

                    return new ParameterInstances(
                        array.Select((value, argumentIndex) => Create(parameterDefinitions, value, read, argumentIndex, summaryStyle))
                            .ToArray());
                }
            }

            if (parameterDefinitions.Length == 1)
            {
                return new ParameterInstances([Create(parameterDefinitions, unwrappedValue, read, argumentIndex: 0, summaryStyle)]);
            }

            throw new NotSupportedException($"Benchmark {benchmark.Name} has invalid type of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]." +
                $" It should be IEnumerable<object[]>, IEnumerable<object>, IAsyncEnumerable<object[]> or IAsyncEnumerable<object>.");
        }

        // Whether the generated code indexes the element it extracts, decided by the interface the source is
        // *written as* and not by which branch above produced this instance: the extraction call is bound against
        // the declared return type, so the index has to be read from the same place the binding is.
        private static bool Indexes(MemberInfo source)
        {
            var returnType = source.GetSourceReturnType();

            return returnType == typeof(IEnumerable<object[]>) || returnType == typeof(IAsyncEnumerable<object[]>);
        }

        private static ParameterInstance Create(ParameterDefinition[] parameterDefinitions, object? value, SourceRead read, int argumentIndex, SummaryStyle summaryStyle)
        {
            var definition = parameterDefinitions[argumentIndex];

            // Asked ahead of the constant path, which renders the value into the source and needs the conversion
            // just as much. Null is left to it: a ref struct is not written as one, and how that is rendered is
            // settled there.
            var takesByRefLike = definition.ParameterType.WithoutRefModifier();

            // InvalidBenchmarkDeclarationException, not InvalidOperationException: BenchmarkRunnerDirty catches this
            // one and reports it as that benchmark's summary, where anything else escapes Run and takes every other
            // benchmark in the call down with it.
            if (value is not null && takesByRefLike.IsByRefLike() && !takesByRefLike.IsStackOnlyWithImplicitCast(value))
                throw new InvalidBenchmarkDeclarationException($"[ArgumentsSource({read.Source.Name})] provides a {value!.GetType().GetDisplayName()}" +
                    $" for the {definition.ParameterType.GetDisplayName()} parameter '{definition.Name}', which has no implicit conversion from it." +
                    $" A by-ref-like parameter only ever takes its value through that conversion, so nothing can be cast to it here." +
                    $" Please, yield a type it converts from - and where the source's element type is a type parameter," +
                    $" the [GenericTypeArguments] in play decide this, so it can hold for one and not the next.");

            if (SourceCodeHelper.IsCompilationTimeConstant(value))
                return new ParameterInstance(definition, new ParameterValue.Constant(value, definition.ParameterType), summaryStyle);

            // A by-ref-like parameter can't be the source's element type, so the value's own type is the one the
            // generated code casts to, relying on its implicit conversion (#774).
            // value is non-null here: null is a compilation-time constant, handled above.
            var targetType = takesByRefLike.IsByRefLike() ? value!.GetType() : definition.ParameterType;

            return new ParameterInstance(
                definition,
                new ParameterValue.FromSource(value, read, Indexes(read.Source) ? argumentIndex : null, targetType),
                summaryStyle);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [UsedImplicitly]
    public static class ParameterExtractor
    {
        public static ValueTask<T> GetParameterAsync<T>(IEnumerable<T> parameters, int index, CancellationToken cancellationToken)
            => GetParameterAsync(parameters.ToAsyncEnumerable(), index, cancellationToken);

        public static async ValueTask<T> GetParameterAsync<T>(IAsyncEnumerable<T> parameters, int index, CancellationToken cancellationToken)
        {
            int count = 0;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await foreach (T parameter in parameters.ConfigureAwait(cancellationToken))
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            {
                if (count == index)
                {
                    return parameter;
                }

                // #1383
                if (parameter is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait();
                }
                else if (parameter is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                count++;
            }

            throw new InvalidOperationException("We should never get here!");
        }
    }
}
