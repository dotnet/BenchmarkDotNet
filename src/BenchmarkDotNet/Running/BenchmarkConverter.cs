using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;

namespace BenchmarkDotNet.Running
{
    public static class BenchmarkConverter
    {
        private const BindingFlags AllMethodsFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static BenchmarkRunInfo TypeToBenchmarks(Type type, IConfig? config = null)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(TypeToBenchmarksAsync(type, config));
        }

        public static ValueTask<BenchmarkRunInfo> TypeToBenchmarksAsync(Type type, IConfig? config = null, CancellationToken cancellationToken = default)
        {
            // Nothing of an open generic type can be read - BenchmarkSwitcher is what substitutes the type arguments -
            // so this is the one declaration error that leaves behind no cases to report it alongside.
            if (type.IsGenericTypeDefinition)
                return new(EmptyRunInfo(type, config, $"{type.Name} is generic type definition, use BenchmarkSwitcher for it"));

            // We should check all methods including private to notify users about private methods with the [Benchmark] attribute
            var benchmarkMethods = GetOrderedBenchmarkMethods(type.GetMethods(AllMethodsFlags));

            return MethodsToBenchmarksWithFullConfig(type, benchmarkMethods, config, cancellationToken);
        }

        public static BenchmarkRunInfo MethodsToBenchmarks(Type containingType, MethodInfo[] benchmarkMethods, IConfig? config = null)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(MethodsToBenchmarksAsync(containingType, benchmarkMethods, config));
        }

        public static ValueTask<BenchmarkRunInfo> MethodsToBenchmarksAsync(Type containingType, MethodInfo[] benchmarkMethods, IConfig? config = null, CancellationToken cancellationToken = default)
            => MethodsToBenchmarksWithFullConfig(containingType, GetOrderedBenchmarkMethods(benchmarkMethods), config, cancellationToken);

        private static MethodInfo[] GetOrderedBenchmarkMethods(MethodInfo[] methods)
            => methods
                .Select(method => (method, attribute: method.ResolveAttribute<BenchmarkAttribute>()))
                .Where(pair => pair.attribute is not null)
                .OrderBy(pair => pair.attribute!.SourceCodeFile)
                .ThenBy(pair => pair.attribute!.SourceCodeLineNumber)
                .Select(pair => pair.method)
                .ToArray();

        private static async ValueTask<BenchmarkRunInfo> MethodsToBenchmarksWithFullConfig(Type type, MethodInfo[] benchmarkMethods, IConfig? config, CancellationToken cancellationToken)
        {
            var allMethods = type.GetMethods(AllMethodsFlags); // benchmarkMethods can be filtered, without Setups, look #564
            var configPerType = GetFullTypeConfig(type, config);

            var declarationErrors = new List<ValidationError>();

            var globalSetupMethods = GetAttributedMethods<GlobalSetupAttribute>(allMethods, "GlobalSetup", declarationErrors);
            var globalCleanupMethods = GetAttributedMethods<GlobalCleanupAttribute>(allMethods, "GlobalCleanup", declarationErrors);
            var iterationSetupMethods = GetAttributedMethods<IterationSetupAttribute>(allMethods, "IterationSetup", declarationErrors);
            var iterationCleanupMethods = GetAttributedMethods<IterationCleanupAttribute>(allMethods, "IterationCleanup", declarationErrors);

            var targets = GetTargets(benchmarkMethods, type, globalSetupMethods, globalCleanupMethods, iterationSetupMethods, iterationCleanupMethods,
                configPerType, declarationErrors);

            var parameterInstances = await GetParameterInstancesAsync(type, configPerType.SummaryStyle, declarationErrors, cancellationToken).ConfigureAwait();

            var benchmarks = new List<BenchmarkCase>();

            bool containsBenchmarkDeclarations = false;

            foreach (var target in targets)
            {
                var argumentsInstances = await GetArgumentsInstancesAsync(target.WorkloadMethod, target.Type, configPerType.SummaryStyle, declarationErrors, cancellationToken).ConfigureAwait();

                var targetParameterInstances =
                    (from parameterInstance in parameterInstances
                     from argumentInstance in argumentsInstances
                     select new ParameterInstances([.. parameterInstance.Items, .. argumentInstance.Items])).ToArray();

                var configPerMethod = GetFullMethodConfig(target.WorkloadMethod, configPerType);

                var benchmarksForTarget =
                    (from job in configPerMethod.GetJobs()
                     from parameterInstance in targetParameterInstances
                     select BenchmarkCase.Create(target, job, parameterInstance, configPerMethod)).ToArray();

                containsBenchmarkDeclarations |= benchmarksForTarget.Length != 0;

                benchmarks.AddRange(GetFilteredBenchmarks(benchmarksForTarget, configPerMethod.GetFilters()));
            }

            var orderedBenchmarks = configPerType.Orderer.GetExecutionOrder([.. benchmarks]).ToArray();
            var compositeInProcessDiagnoser = new Diagnosers.CompositeInProcessDiagnoser([.. configPerType.GetDiagnosers().OfType<Diagnosers.IInProcessDiagnoser>()]);

            // Collapse duplicate errors.
            return new BenchmarkRunInfo(orderedBenchmarks, type, configPerType, containsBenchmarkDeclarations, compositeInProcessDiagnoser, [.. declarationErrors.Distinct()]);
        }

        private static BenchmarkRunInfo EmptyRunInfo(Type type, IConfig? config, string error)
        {
            var configPerType = GetFullTypeConfig(type, config);

            return new BenchmarkRunInfo(
                [],
                type,
                configPerType,
                containsBenchmarkDeclarations: false,
                new Diagnosers.CompositeInProcessDiagnoser([.. configPerType.GetDiagnosers().OfType<Diagnosers.IInProcessDiagnoser>()]),
                [new ValidationError(isCritical: true, error)]);
        }

        private static ImmutableConfig GetFullTypeConfig(Type type, IConfig? config)
        {
            config = config ?? DefaultConfig.Instance;

            var typeAttributes = type.GetCustomAttributes(true).OfType<IConfigSource>();
            var assemblyAttributes = type.Assembly.GetCustomAttributes().OfType<IConfigSource>();

            foreach (var configFromAttribute in assemblyAttributes.Concat(typeAttributes))
                config = ManualConfig.Union(config, configFromAttribute.Config);

            return ImmutableConfigBuilder.Create(config);
        }

        private static ImmutableConfig GetFullMethodConfig(MethodInfo method, ImmutableConfig typeConfig)
        {
            var methodAttributes = method.GetCustomAttributes(true).OfType<IConfigSource>();

            if (!methodAttributes.Any()) // the most common case
                return typeConfig;

            var config = ManualConfig.Create(typeConfig);
            foreach (var configFromAttribute in methodAttributes)
                config = ManualConfig.Union(config, configFromAttribute.Config);

            return ImmutableConfigBuilder.Create(config);
        }

        private static Descriptor[] GetTargets(
            MethodInfo[] targetMethods,
            Type type,
            Tuple<MethodInfo, TargetedAttribute>[] globalSetupMethods,
            Tuple<MethodInfo, TargetedAttribute>[] globalCleanupMethods,
            Tuple<MethodInfo, TargetedAttribute>[] iterationSetupMethods,
            Tuple<MethodInfo, TargetedAttribute>[] iterationCleanupMethods,
            IConfig config,
            List<ValidationError> declarationErrors)
        {
            return targetMethods
                .Where(methodInfo => IsCallable("Benchmark", methodInfo, declarationErrors))
                .Select(methodInfo => CreateDescriptor(type,
                                                   GetTargetedMatchingMethod(methodInfo, globalSetupMethods),
                                                   methodInfo,
                                                   GetTargetedMatchingMethod(methodInfo, globalCleanupMethods),
                                                   GetTargetedMatchingMethod(methodInfo, iterationSetupMethods),
                                                   GetTargetedMatchingMethod(methodInfo, iterationCleanupMethods),
                                                   methodInfo.ResolveAttribute<BenchmarkAttribute>()!,
                                                   targetMethods,
                                                   config))
                .ToArray();
        }

        private static MethodInfo? GetTargetedMatchingMethod(MethodInfo benchmarkMethod, Tuple<MethodInfo, TargetedAttribute>[] methods)
            => methods.Where(method => method.Item2.Match(benchmarkMethod)).Select(method => method.Item1).FirstOrDefault();

        private static Tuple<MethodInfo, TargetedAttribute>[] GetAttributedMethods<T>(MethodInfo[] methods, string methodName, List<ValidationError> declarationErrors) where T : TargetedAttribute
        {
            return methods
                .Select(method => (method, attributes: method.GetCustomAttributes<T>().ToArray()))
                .Where(pair => pair.attributes.Length > 0 && IsCallable(methodName, pair.method, declarationErrors))
                .SelectMany(pair => pair.attributes.Select(attr => new Tuple<MethodInfo, TargetedAttribute>(pair.method, attr)))
                .OrderByDescending(x => x.Item2.Targets?.Length ?? 0).ToArray();
        }

        private static Descriptor CreateDescriptor(
            Type type,
            MethodInfo? globalSetupMethod,
            MethodInfo methodInfo,
            MethodInfo? globalCleanupMethod,
            MethodInfo? iterationSetupMethod,
            MethodInfo? iterationCleanupMethod,
            BenchmarkAttribute attr,
            MethodInfo[] targetMethods,
            IConfig config)
        {
            var categoryDiscoverer = config.CategoryDiscoverer ?? DefaultCategoryDiscoverer.Instance;
            var target = new Descriptor(
                type,
                methodInfo,
                globalSetupMethod,
                globalCleanupMethod,
                iterationSetupMethod,
                iterationCleanupMethod,
                attr.Description,
                baseline: attr.Baseline,
                categories: categoryDiscoverer.GetCategories(methodInfo),
                operationsPerInvoke: attr.OperationsPerInvoke,
                methodIndex: Array.IndexOf(targetMethods, methodInfo));
            return target;
        }

        private static async ValueTask<IReadOnlyList<ParameterInstances>> GetParameterInstancesAsync(Type type, SummaryStyle summaryStyle, List<ValidationError> declarationErrors, CancellationToken cancellationToken)
        {
            List<ParameterValues> GetValues<TAttribute>(Func<TAttribute, ParameterDefinition, IReadOnlyList<ParameterValue>> getValidValues) where TAttribute : PriorityAttribute
                => [.. type.GetTypeMembersWithGivenAttribute<TAttribute>(ReflectionExtensions.ParameterMemberFlags)
                    .Select(member =>
                    {
                        var definition = new ParameterDefinition(member.Name, member.IsStatic, isArgument: false, member.ParameterType, member.Attribute.Priority);

                        return new ParameterValues(definition, getValidValues(member.Attribute, definition));
                    })];

            var parameters = GetValues<ParamsAttribute>((attribute, definition) => GetValidValues(attribute.Values, definition, "[Params]", declarationErrors));

            // A member whose source could not be read is left out, so the others still range over their values.
            foreach (var member in type.GetTypeMembersWithGivenAttribute<ParamsSourceAttribute>(ReflectionExtensions.ParameterMemberFlags))
            {
                var targetType = member.Attribute.Type ?? type;

                if (await GetValidValuesForParamsSourceAsync(targetType, member.Attribute.Name, declarationErrors, cancellationToken).ConfigureAwait() is not { } read)
                    continue;

                var definition = new ParameterDefinition(member.Name, member.IsStatic, isArgument: false, member.ParameterType, member.Attribute.Priority);

                parameters.Add(new ParameterValues(definition, SmartParamBuilder.CreateForParams(definition, read.source, read.values, declarationErrors)));
            }
            parameters.AddRange(GetValues<ParamsAllValuesAttribute>((_, definition) =>
                GetValidValues(GetAllValidValues(definition.ParameterType), definition, "[ParamsAllValues]", declarationErrors)));

            // Each member ranges over its values independently, so the cases are their cartesian product: every case so far is re-made once per value
            // of the next parameter. The seed is the single empty case, which is also the answer for a benchmark that has no parameters at all.
            List<ParameterInstances> cases = [ParameterInstances.Empty];
            List<ParameterInstances>? expanded = null;
            foreach (var parameter in parameters)
            {
                expanded ??= [];
                expanded.Clear();
#if NET6_0_OR_GREATER
                expanded.EnsureCapacity(cases.Count * parameter.Items.Count);
#endif
                foreach (var instances in cases)
                {
                    foreach (var value in parameter.Items)
                    {
                        expanded.Add(new ParameterInstances([.. instances.Items, new(parameter.Definition, value, summaryStyle)]));
                    }
                }
                (cases, expanded) = (expanded, cases);
            }

            return cases;
        }

        /// <summary>
        /// One argument written into the benchmark's own attribute, and the type the generated code holds it as -
        /// or null where the parameter has no such value. That type is the parameter's own, except for a
        /// by-ref-like one, which no field can hold (#774): it is reached through the conversion the value's own
        /// type declares, so only the value names what holds it - which is also why a null has to be refused
        /// before this can ask.
        /// </summary>
        private static ParameterInstance? Argument(MethodInfo benchmark, ParameterDefinition definition, object? value, SummaryStyle summaryStyle, List<ValidationError> declarationErrors)
        {
            if (SmartParamBuilder.RefusesNull($"[Arguments] on {benchmark.Name}", definition, value, declarationErrors))
                return null;

            var sourceType = definition.ParameterType.WithoutRefModifier().IsByRefLike() ? value!.GetType() : definition.ParameterType;
            return new ParameterInstance(definition, new ParameterValue.Constant(value, sourceType), summaryStyle);
        }

        private static async ValueTask<IReadOnlyList<ParameterInstances>> GetArgumentsInstancesAsync(MethodInfo benchmark, Type benchmarkType, SummaryStyle summaryStyle, List<ValidationError> declarationErrors, CancellationToken cancellationToken)
        {
            int priority = benchmark.GetCustomAttributes<PriorityAttribute>().Sum(attribute => attribute.Priority);

            var parameterDefinitions = benchmark.GetParameters()
                .Select(parameter => new ParameterDefinition(parameter.Name!, isStatic: false, isArgument: true, parameter.ParameterType, priority))
                .ToArray();

            if (parameterDefinitions.Length == 0)
            {
                return [ParameterInstances.Empty];
            }

            var result = new List<ParameterInstances>();
            foreach (var argumentsAttribute in benchmark.GetCustomAttributes<ArgumentsAttribute>())
            {
                if (parameterDefinitions.Length != argumentsAttribute.Values.Length)
                {
                    declarationErrors.Add(new ValidationError(isCritical: true,
                        $"Benchmark {benchmark.Name} has invalid number of defined arguments provided with [Arguments]! {argumentsAttribute.Values.Length} instead of {parameterDefinitions.Length}."));

                    continue;
                }

                // Every argument is read before the row is judged, so one the parameter refuses does not hide the
                // next - and a row missing any of them is left out rather than reported twice.
                var arguments = argumentsAttribute.Values
                    .Select((value, index) => Argument(benchmark, parameterDefinitions[index], value, summaryStyle, declarationErrors))
                    .ToArray();

                if (arguments.All(argument => argument is not null))
                    result.Add(new ParameterInstances(arguments!));
            }

            if (!benchmark.HasAttribute<ArgumentsSourceAttribute>())
                return result;

            var argumentsSourceAttribute = benchmark.GetCustomAttribute<ArgumentsSourceAttribute>()!;
            var targetType = argumentsSourceAttribute.Type ?? benchmarkType;

            if (await GetValidValuesForParamsSourceAsync(targetType, argumentsSourceAttribute.Name, declarationErrors, cancellationToken).ConfigureAwait() is not { } valuesInfo)
                return result;

            for (int sourceIndex = 0; sourceIndex < valuesInfo.values.Length; sourceIndex++)
            {
                if (SmartParamBuilder.CreateForArguments(benchmark, parameterDefinitions, valuesInfo, sourceIndex, summaryStyle, declarationErrors) is { } instances)
                    result.Add(instances);
            }

            return result;
        }

        private static ImmutableArray<BenchmarkCase> GetFilteredBenchmarks(BenchmarkCase[] benchmarks, IEnumerable<IFilter> filters)
            => [.. benchmarks.Where(benchmark => filters.All(filter => filter.Predicate(benchmark)))];

        /// <summary>
        /// Whether a method BenchmarkDotNet is going to call is one it can call. Every problem a method has is
        /// reported rather than only the first, and the method is then left out - which costs only the cases it
        /// would have produced, so the rest of the type is still read.
        /// </summary>
        private static bool IsCallable(string methodType, MethodInfo methodInfo, List<ValidationError> declarationErrors)
        {
            int before = declarationErrors.Count;

            if (methodInfo.GetParameters().Any() && !methodInfo.HasAttribute<ArgumentsAttribute>() && !methodInfo.HasAttribute<ArgumentsSourceAttribute>())
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"{methodType} method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments."));

            /* Moved the code that verifies if DeclaringType of a given MethodInfo (a method) is publicly accessible to CompilationValidator */
            if (!methodInfo.IsPublic)
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"{methodType} method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public."));

            if (methodInfo.IsGenericMethod)
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"{methodType} method {methodInfo.Name} is generic.\nGeneric {methodType} methods are not supported."));

            return declarationErrors.Count == before;
        }

        // Every value is asked, so one the parameter refuses does not hide the next.
        private static IReadOnlyList<ParameterValue> GetValidValues(object?[] values, ParameterDefinition definition, string origin, List<ValidationError> declarationErrors)
            => [.. values
                .Where(value => !SmartParamBuilder.RefusesNull(origin, definition, value, declarationErrors))
                .Select(value => new ParameterValue.Constant(value, definition.ParameterType))];

        private static async ValueTask<(MemberInfo source, object?[] values)?> GetValidValuesForParamsSourceAsync(Type sourceType, string sourceName, List<ValidationError> declarationErrors, CancellationToken cancellationToken)
        {
            var source = sourceType.FindSourceMember(sourceName);

            if (source == null)
            {
                declarationErrors.Add(NoSourceMemberFound(sourceType, sourceName));
                return null;
            }

            // A source method may have parameters as long as they are all optional (e.g. an async iterator with an
            // [EnumeratorCancellation] CancellationToken); we invoke it with their default values.
            object? sourceValue = source is MethodInfo method
                ? method.Invoke(method.IsStatic ? null : Activator.CreateInstance(sourceType), GetDefaultArguments(method))
                : ((PropertyInfo) source).GetValue(((PropertyInfo) source).GetMethod!.IsStatic ? null : Activator.CreateInstance(sourceType)!);

            return await ToArrayAsync(sourceValue, source, sourceType, declarationErrors, cancellationToken).ConfigureAwait() is { } values
                ? (source, values)
                : null;
        }

        private static ValidationError NoSourceMemberFound(Type sourceType, string sourceName)
        {
            var namedMethods = sourceType.GetAllMethods().Where(method => method.Name == sourceName && method.IsPublic).ToArray();

            if (namedMethods.Any(method => method.IsGenericMethodDefinition))
                return new ValidationError(isCritical: true, $"Source method {sourceName} of type {sourceType.GetDisplayName()} is generic.\nGeneric source methods are not supported.");

            return new ValidationError(isCritical: true, namedMethods.Length > 0
                ? $"{sourceType.Name}.{sourceName} has required parameters, unable to read values for [ParamsSource]/[ArgumentsSource]. A source method must be parameterless or have only optional parameters."
                : $"{sourceType.Name} has no public, accessible method/property called {sourceName}, unable to read values for [ParamsSource].");
        }

        // Default argument values for an all-optional-parameter source method. A parameter can be optional without declaring
        // a default ([Optional] with no [DefaultParameterValue]), and MethodInfo.Invoke(object, object[]) does no optional-parameter
        // binding - so we pass default(T), which is what the C# compiler passes at a call site that omits the argument.
        private static object?[]? GetDefaultArguments(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return null;

            var arguments = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                arguments[i] = parameters[i].GetDefaultArgumentValue();
            return arguments;
        }

        private static async ValueTask<object?[]?> ToArrayAsync(object? sourceValue, MemberInfo memberInfo, Type type, List<ValidationError> declarationErrors, CancellationToken cancellationToken)
        {
            var sourceType = memberInfo is MethodInfo methodInfo
                ? methodInfo.ReturnType
                : ((PropertyInfo) memberInfo).PropertyType;

            // Checked before the shape, so a null async source reports the same declaration error a null
            // synchronous source does instead of failing while being enumerated.
            if (sourceValue == null)
            {
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"{memberInfo.Name} of type {type.Name} returned null, unable to read values for [ParamsSource]/[ArgumentsSource]."));

                return null;
            }

            // Reading the values puts each into an object[], which a ref struct cannot enter - the enumeration fails
            // inside reflection saying nothing about the benchmark. Expressible since .NET 10 gave IEnumerable<T> an
            // allows-ref-struct type parameter. Asked of both shapes, and so ahead of either: an async source reads
            // its values into the same object[]. SourceReturnTypeValidator reports the declaration this substitutes.
            if (memberInfo.GetSourceReturnType().TryGetSourceElementType(out var refLikeCandidate) && refLikeCandidate.IsByRefLike())
            {
                declarationErrors.Add(new ValidationError(isCritical: true,
                    $"{type.Name}.{memberInfo.Name} yields {refLikeCandidate.GetDisplayName()}, which is a ref struct, and BenchmarkDotNet cannot read a value into one."
                    + " Please, yield what the value is built from - IEnumerable<byte[]> for a ReadOnlySpan<byte> parameter - and let the benchmark take the ref struct."));

                return null;
            }

            // Only IAsyncEnumerable<T> is supported for async sources (not the await-foreach pattern). Decided from
            // the declared type, and before the synchronous check, because that is what the generated code binds:
            // an async-declared source whose value also implements IEnumerable must not be read synchronously here.
            if (sourceType.IsIAsyncEnumerable(out var elementType))
            {
                List<object?> items = [];
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                await foreach (var item in DynamicAwaitHelper.EnumerateSourceAsync(sourceValue, elementType).ConfigureAwait(cancellationToken))
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                {
                    items.Add(item);
                }
                return [.. items];
            }

            // Synchronous sources are matched on the value: the declared type is often looser than what is returned
            // (e.g. a non-generic IEnumerable), and the generated code binds the IEnumerable<T> overload either way.
            if (sourceValue is IEnumerable collection)
                return [.. collection];

            declarationErrors.Add(new ValidationError(isCritical: true,
                $"{memberInfo.Name} of type {type.Name} does not implement IEnumerable or IAsyncEnumerable<T>, unable to read values for [ParamsSource]"));

            return null;
        }

        private static object?[] GetAllValidValues(Type parameterType)
        {
            if (parameterType == typeof(bool))
                return [false, true];

            if (parameterType.GetTypeInfo().IsEnum)
            {
                if (parameterType.GetTypeInfo().IsDefined(typeof(FlagsAttribute)))
                    return [Activator.CreateInstance(parameterType)];

                return [.. Enum.GetValues(parameterType).Cast<object>()];
            }

            var nullableUnderlyingType = Nullable.GetUnderlyingType(parameterType);
            if (nullableUnderlyingType != null)
                return [null, .. GetAllValidValues(nullableUnderlyingType)];

            return [Activator.CreateInstance(parameterType)];
        }
    }
}
