using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Parameters
{
    internal static class SmartParamBuilder
    {
        /// <summary>
        /// Whether the parameter refuses this null, saying so where it does. Asked only where nothing but the
        /// value names a type - an attribute's argument, or a source declared to yield object - because everywhere
        /// else the declaration was judged already, and a null read from a source of some other element type says
        /// the value could not be read rather than that it was null: a ref struct cannot be boxed to be looked at.
        /// <para>
        /// The two toolchains do not agree on a null the parameter cannot hold: the generated code refuses to
        /// compile it - CS0037, or CS8345 where a by-ref-like parameter would have to become a field - while the
        /// emitted one substitutes the type's default and runs a case the author never wrote. An analyzer reports
        /// this where the benchmark is written; a host that runs none has only this.
        /// </para>
        /// </summary>
        internal static bool RefusesNull(string origin, ParameterDefinition definition, object? value, List<ValidationError> declarationErrors)
        {
            if (value is not null || !definition.ParameterType.RefusesNull())
                return false;

            declarationErrors.Add(new ValidationError(isCritical: true,
                $"{origin} provides null for the {definition.ParameterType.WithoutRefModifier().GetDisplayName()}" +
                $" {(definition.IsArgument ? "parameter" : "member")} '{definition.Name}', which is a value type" +
                $" - null is not one of its values."));

            return true;
        }

        internal static IReadOnlyList<ParameterValue> CreateForParams(ParameterDefinition definition, MemberInfo source, object?[] values, List<ValidationError> declarationErrors)
        {
            var parameterType = definition.ParameterType;

            // A one-element object[] around a constant is unwrapped to the constant, which is then rendered inline
            // and needs no index. Only around a constant: a value that has to be read back from the source keeps
            // the whole array, because the generated code emits no index here to reach inside one with, and
            // unwrapping only the in-process side would have the two toolchains assign different values.
            if (values.All(value => value is object[] { Length: 1 } wrapper && SourceCodeHelper.IsCompilationTimeConstant(wrapper[0])))
                values = values.Select(value => ((object[]) value!)[0]).ToArray();

            bool namesNothing = source.GetSourceReturnType().TryGetSourceElementType(out var elementType) && elementType == typeof(object);

            // The index is the value's place in the source, so a refused value is filtered out carrying it rather
            // than before it is known - the ones that remain still name where they are read back from.
            return [.. values
                .Select((value, index) => (value, index))
                .Where(item => !namesNothing || !RefusesNull($"[ParamsSource({source.Name})]", definition, item.value, declarationErrors))
                .Select(item => SourceCodeHelper.IsCompilationTimeConstant(item.value)
                    ? (ParameterValue) new ParameterValue.Constant(item.value, parameterType)
                    : new ParameterValue.FromSource(item.value, new SourceRead(source, item.index), elementIndex: null, parameterType))];
        }

        /// <summary>
        /// One row of a source, as the case it feeds - or null where it feeds none, which is said rather than
        /// thrown so the rows and benchmarks around it are still read.
        /// </summary>
        internal static ParameterInstances? CreateForArguments(
            MethodInfo benchmark,
            ParameterDefinition[] parameterDefinitions,
            (MemberInfo source, object?[] values) valuesInfo,
            int sourceIndex,
            SummaryStyle summaryStyle,
            List<ValidationError> declarationErrors)
        {
            var unwrappedValue = valuesInfo.values[sourceIndex];

            // One read for the whole row: every parameter below takes its value out of this one, so the generated
            // code enumerates the source once per case rather than once per argument.
            var read = new SourceRead(valuesInfo.source, sourceIndex);

            // Read from the declaration, never from the value. The generated code binds its extraction against the
            // declared element type and reaches inside whatever that names, so a decision made from the runtime
            // shape would let the two toolchains take different arguments out of the same row.
            if (!CanFeed(benchmark, parameterDefinitions, valuesInfo.source, declarationErrors, out var declared, out var items))
                return null;

            if (items is null)
                return Row([Create(parameterDefinitions, unwrappedValue, declared[0], read, argumentIndex: 0, isArgumentList: false, summaryStyle, declarationErrors)]);

            // Guards a value that does not honour its own declaration; the reading itself was settled above.
            if (unwrappedValue is null)
            {
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"Benchmark {benchmark.Name} expects an argument list from [ArgumentsSource({valuesInfo.source.Name})], but null was provided."));
                return null;
            }

            if (items(unwrappedValue) is not { } arguments)
                return null;

            if (parameterDefinitions.Length != arguments.Length)
            {
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"Benchmark {benchmark.Name} has invalid number of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]!" +
                    $" {arguments.Length} instead of {parameterDefinitions.Length}."));
                return null;
            }

            return Row([.. arguments.Select((value, argumentIndex) =>
                Create(parameterDefinitions, value, declared[argumentIndex], read, argumentIndex, isArgumentList: true, summaryStyle, declarationErrors))]);
        }

        // A row is a case only where every one of its arguments could be read. Each that could not has already said
        // why - all of them are asked, so one bad argument does not hide the next.
        private static ParameterInstances? Row(ParameterInstance?[] arguments)
            => arguments.All(argument => argument is not null) ? new ParameterInstances(arguments!) : null;

        // Whether a source can feed the benchmark at all, saying why where it cannot. Where it can, `items` takes a
        // row apart the same way the generated code will, or is null where each element is one argument rather than
        // a list - decided from the *declared* element type. `declared` names what each argument is declared as,
        // which is what the rule was decided from and so what the rest of discovery asks its questions of.
        private static bool CanFeed(
            MethodInfo benchmark,
            ParameterDefinition[] parameterDefinitions,
            MemberInfo source,
            List<ValidationError> declarationErrors,
            out Type[] declared,
            out Func<object, object?[]?>? items)
        {
            items = null;

            // No single element type says nothing about the values, and the values must not stand in for it.
            // SourceReturnTypeValidator reports that; treating it as one argument lets discovery get that far -
            // with object standing in, which is the declaration that says nothing and defers to the value.
            if (!source.GetSourceReturnType().TryGetSourceElementType(out var elementType))
            {
                declared = [typeof(object)];
                return true;
            }

            var parameters = parameterDefinitions.Select(definition => definition.ParameterType).ToArray();

            if (!Admits(elementType, parameters, out var reading, out declared))
            {
                declarationErrors.Add(new ValidationError(isCritical: true, CannotFeed(benchmark, source, elementType, parameters)));
                return false;
            }

            // A row is read through the non-generic IEnumerable, which a type is free to implement differently from
            // the IEnumerable<T> it declares, so what arrives is not always the shape the declaration promised.
            // Each reading says what it needs of a row before reaching into it, and names what it got instead.
            object?[]? NotAnArgumentList(object row)
            {
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"Benchmark {benchmark.Name} expects an argument list from [ArgumentsSource({source.Name})]," +
                    $" but {row.GetType().GetDisplayName()} was provided."));

                return null;
            }

            switch (reading)
            {
                // Not the exact type: an object[] parameter list legitimately arrives as any array of a reference
                // type, which array covariance makes one.
                case ElementReading.ArgumentArray:
                    items = row => row is Array { Rank: 1 } array
                        ? [.. Enumerable.Range(0, array.Length).Select(item => array.GetValue(item))]
                        : NotAnArgumentList(row);
                    return true;

                // The exact type, because a ValueTuple is a struct: nothing else is one, and the items the walk
                // below reaches were read off this very type.
                case ElementReading.ArgumentTuple:
                    var paths = Enumerable.Range(0, parameters.Length).Select(TupleItemPath).ToArray();
                    items = row => row.GetType() == elementType
                        ? [.. paths.Select(path => TupleItem(row, path))]
                        : NotAnArgumentList(row);
                    return true;

                default:
                    return true;
            }
        }

        private enum ElementReading { SingleValue, ArgumentArray, ArgumentTuple }

        // Whether a source declared to yield this element can feed these parameters, and if so how a row is read.
        //
        // Nothing here is cast that C# might refuse, because nothing is cast except out of `object`, which every
        // value enters and which comes back out to anything that is not a ref struct. That is the whole of the
        // rule: name what the parameter takes, or name object and let the cast be trivial. It is deliberately not
        // a model of the language's conversions - reflection answers the CLR's rules, which admit conversions C#
        // refuses and refuse conversions C# admits, so such a model disagrees with the compiler in both
        // directions.
        private static bool Admits(Type elementType, Type[] parameters, out ElementReading reading, out Type[] declared)
        {
            reading = ElementReading.SingleValue;
            declared = [elementType];

            // One parameter takes the value itself, never a one-element list wrapping it: a declaration cannot
            // tell that wrapper apart from an argument list for a benchmark that happens to take one argument.
            if (parameters.Length == 1)
                return Admissible(elementType, parameters[0]) || elementType == typeof(object);

            // object[] is the shape the documentation has always shown for several arguments, and every value
            // enters it. A ValueTuple says the same with the types written down, which is what BDN1508 asks for.
            if (elementType == typeof(object[]))
            {
                reading = ElementReading.ArgumentArray;

                // Every item of an object[] is declared object, whatever it happens to hold.
                declared = Enumerable.Repeat(typeof(object), parameters.Length).ToArray();
                return true;
            }

            if (IsArgumentTuple(elementType, parameters.Length, out var tupleItems)
                && tupleItems.Zip(parameters, Admissible).All(admissible => admissible))
            {
                reading = ElementReading.ArgumentTuple;
                declared = tupleItems;
                return true;
            }

            return false;
        }

        // A type is admissible for a parameter when it names what the parameter takes. A by-ref-like parameter is
        // reached only through a conversion operator, so naming the type that operator takes is how it is named -
        // and the generated code writes the cast itself, so an explicit operator serves as well as an implicit one.
        private static bool Admissible(Type type, Type parameter)
        {
            var takes = parameter.WithoutRefModifier();

            return takes == type || (takes.IsByRefLike() && takes.TakesByConversion(type));
        }

        // From the eighth item a ValueTuple nests its tail in TRest, which is another ValueTuple - so an arity above
        // seven is read by walking that chain rather than refused, and `(a, b, c, d, e, f, g, h)` feeds eight
        // parameters as readily as a shorter one feeds its own.
        private static bool IsArgumentTuple(Type elementType, int arity, out Type[] items)
        {
            items = [];

            if (arity < 2)
                return false;

            var collected = new List<Type>();

            for (var tuple = elementType; ;)
            {
                if (!tuple.IsGenericType)
                    return false;

                var definition = tuple.GetGenericTypeDefinition();
                if (definition.Namespace != "System" || !definition.Name.StartsWith("ValueTuple`", StringComparison.Ordinal))
                    return false;

                var arguments = tuple.GetGenericArguments();

                if (arguments.Length != TupleRestArity)
                {
                    collected.AddRange(arguments);
                    break;
                }

                collected.AddRange(arguments.Take(TupleRestArity - 1));
                tuple = arguments[TupleRestArity - 1];
            }

            if (collected.Count != arity)
                return false;

            items = collected.ToArray();
            return true;
        }

        // ValueTuple`8 is the nesting one: seven items and a TRest holding the remainder.
        private const int TupleRestArity = 8;

        private static object? TupleItem(object row, string[] path)
        {
            object? current = row;

            foreach (var member in path)
            {
                current = (current?.GetType().GetField(member)
                        ?? throw new InvalidOperationException($"Bug: {current?.GetType().GetDisplayName() ?? "null"} has no {member}."))
                    .GetValue(current);
            }

            return current;
        }

        // The members reaching one item of a ValueTuple, as the generated code and discovery both walk them, so the
        // two cannot disagree about which item an index names.
        internal static string[] TupleItemPath(int index)
        {
            var path = new List<string>();

            for (; index >= TupleRestArity - 1; index -= TupleRestArity - 1)
                path.Add("Rest");

            path.Add($"Item{index + 1}");

            return path.ToArray();
        }

        private static string CannotFeed(MethodInfo benchmark, MemberInfo source, Type elementType, Type[] parameters)
        {
            string takes = string.Join(", ", parameters.Select(parameter => parameter.WithoutRefModifier().GetDisplayName()));

            return $"Benchmark {benchmark.Name} cannot be fed from [ArgumentsSource({source.Name})]," +
                $" which is declared to yield {elementType.GetDisplayName()} for" +
                $" {(parameters.Length == 1 ? $"a {takes} argument" : $"({takes})")}." +
                (parameters.Length == 1
                    ? $" Please, declare the source to yield {takes} or object." +
                      $" A benchmark taking one argument is fed the value itself, never a one-element array wrapping it."
                    : $" Please, declare the source to yield ({takes}) or object[].");
        }

        private static ParameterInstance? Create(
            ParameterDefinition[] parameterDefinitions,
            object? value,
            Type declared,
            SourceRead read,
            int argumentIndex,
            bool isArgumentList,
            SummaryStyle summaryStyle,
            List<ValidationError> declarationErrors)
        {
            var definition = parameterDefinitions[argumentIndex];


            // What the argument is, as far as anything here can know: the type its declaration names. object is the
            // one declaration that names nothing - every value enters it - so that is the only one the value itself
            // answers for. Asking the value otherwise would ask a different question than the rule that admitted
            // the source: a conversion is declared for exactly one type, so a value of a derived type would be
            // refused by the very rule its declaration passed.
            if (declared == typeof(object) && RefusesNull($"[ArgumentsSource({read.Source.Name})]", definition, value, declarationErrors))
                return null;

            // Where the value is null the parameter takes it - only one that cannot was refused just above - so
            // object stands for itself and nothing is cast to a by-ref-like parameter it could not reach.
            var provides = declared == typeof(object) ? value?.GetType() ?? typeof(object) : declared;

            // Asked ahead of the constant path, which writes the value into the generated source and needs a type
            // that can hold it just as much - null included, which is why this no longer waits for a value.
            var takesByRefLike = definition.ParameterType.WithoutRefModifier();

            if (takesByRefLike.IsByRefLike() && !takesByRefLike.TakesByConversion(provides))
            {
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"[ArgumentsSource({read.Source.Name})] provides a {provides.GetDisplayName()}" +
                    $" for the {definition.ParameterType.GetDisplayName()} parameter '{definition.Name}', which has no conversion from it." +
                    $" A by-ref-like parameter only ever takes its value through such a conversion, so nothing can be cast to it here." +
                    $" Please, yield a type it converts from - and where the source's element type is a type parameter," +
                    $" the [GenericTypeArguments] in play decide this, so it can hold for one and not the next."));

                return null;
            }

            // A by-ref-like parameter can't be stored in a field, so the generated code holds the value as the type
            // it is declared as and converts on load (#774) - which is the type written here, and the one the cast
            // below has to name.
            var holds = takesByRefLike.IsByRefLike() ? provides : definition.ParameterType;

            if (SourceCodeHelper.IsCompilationTimeConstant(value))
                return new ParameterInstance(definition, new ParameterValue.Constant(value, holds), summaryStyle);

            return new ParameterInstance(
                definition,
                new ParameterValue.FromSource(value, read, isArgumentList ? argumentIndex : null, holds),
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
