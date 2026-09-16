using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
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
            if (type.IsGenericTypeDefinition)
                throw new InvalidBenchmarkDeclarationException($"{type.Name} is generic type definition, use BenchmarkSwitcher for it"); // for "open generic types" should be used BenchmarkSwitcher

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

            var globalSetupMethods = GetAttributedMethods<GlobalSetupAttribute>(allMethods, "GlobalSetup");
            var globalCleanupMethods = GetAttributedMethods<GlobalCleanupAttribute>(allMethods, "GlobalCleanup");
            var iterationSetupMethods = GetAttributedMethods<IterationSetupAttribute>(allMethods, "IterationSetup");
            var iterationCleanupMethods = GetAttributedMethods<IterationCleanupAttribute>(allMethods, "IterationCleanup");

            var targets = GetTargets(benchmarkMethods, type, globalSetupMethods, globalCleanupMethods, iterationSetupMethods, iterationCleanupMethods,
                configPerType).ToArray();

            var parameterInstances = await GetParameterInstancesAsync(type, configPerType.SummaryStyle, cancellationToken).ConfigureAwait();

            var benchmarks = new List<BenchmarkCase>();

            bool containsBenchmarkDeclarations = false;

            foreach (var target in targets)
            {
                var argumentsInstances = await GetArgumentsInstancesAsync(target.WorkloadMethod, target.Type, configPerType.SummaryStyle, cancellationToken).ConfigureAwait();

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

            return new BenchmarkRunInfo(orderedBenchmarks, type, configPerType, containsBenchmarkDeclarations, compositeInProcessDiagnoser);
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

        private static IEnumerable<Descriptor> GetTargets(
            MethodInfo[] targetMethods,
            Type type,
            Tuple<MethodInfo, TargetedAttribute>[] globalSetupMethods,
            Tuple<MethodInfo, TargetedAttribute>[] globalCleanupMethods,
            Tuple<MethodInfo, TargetedAttribute>[] iterationSetupMethods,
            Tuple<MethodInfo, TargetedAttribute>[] iterationCleanupMethods,
            IConfig config)
        {
            return targetMethods
                .Select(methodInfo => CreateDescriptor(type,
                                                   GetTargetedMatchingMethod(methodInfo, globalSetupMethods),
                                                   methodInfo,
                                                   GetTargetedMatchingMethod(methodInfo, globalCleanupMethods),
                                                   GetTargetedMatchingMethod(methodInfo, iterationSetupMethods),
                                                   GetTargetedMatchingMethod(methodInfo, iterationCleanupMethods),
                                                   methodInfo.ResolveAttribute<BenchmarkAttribute>()!,
                                                   targetMethods,
                                                   config));
        }

        private static MethodInfo? GetTargetedMatchingMethod(MethodInfo benchmarkMethod, Tuple<MethodInfo, TargetedAttribute>[] methods)
            => methods.Where(method => method.Item2.Match(benchmarkMethod)).Select(method => method.Item1).FirstOrDefault();

        private static Tuple<MethodInfo, TargetedAttribute>[] GetAttributedMethods<T>(MethodInfo[] methods, string methodName) where T : TargetedAttribute
        {
            return methods.SelectMany(m => m.GetCustomAttributes<T>()
                .Select(attr =>
                {
                    AssertMethodIsAccessible(methodName, m);
                    AssertMethodHasCorrectSignature(methodName, m);
                    AssertMethodIsNotGeneric(methodName, m);

                    return new Tuple<MethodInfo, TargetedAttribute>(m, attr);
                })).OrderByDescending(x => x.Item2.Targets?.Length ?? 0).ToArray();
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
            AssertMethodHasCorrectSignature("Benchmark", methodInfo);
            AssertMethodIsAccessible("Benchmark", methodInfo);
            AssertMethodIsNotGeneric("Benchmark", methodInfo);
            return target;
        }

        private static async ValueTask<IReadOnlyList<ParameterInstances>> GetParameterInstancesAsync(Type type, SummaryStyle summaryStyle, CancellationToken cancellationToken)
        {
            IEnumerable<ParameterValues> GetValues<TAttribute>(Func<TAttribute, Type, IReadOnlyList<ParameterValue>> getValidValues) where TAttribute : PriorityAttribute
                => type.GetTypeMembersWithGivenAttribute<TAttribute>(ReflectionExtensions.ParameterMemberFlags)
                    .Select(member =>
                        new ParameterValues(
                            new(member.Name, member.IsStatic, isArgument: false, member.ParameterType, member.Attribute.Priority),
                            getValidValues(member.Attribute, member.ParameterType)
                        )
                    );

            var parameters = GetValues<ParamsAttribute>((attribute, parameterType) => GetValidValues(attribute.Values, parameterType)).ToList();
            foreach (var member in type.GetTypeMembersWithGivenAttribute<ParamsSourceAttribute>(ReflectionExtensions.ParameterMemberFlags))
            {
                var targetType = member.Attribute.Type ?? type;
                var (source, values) = await GetValidValuesForParamsSourceAsync(targetType, member.Attribute.Name, cancellationToken).ConfigureAwait();
                parameters.Add(new ParameterValues(
                    new ParameterDefinition(member.Name, member.IsStatic, isArgument: false, member.ParameterType, member.Attribute.Priority),
                    SmartParamBuilder.CreateForParams(member.ParameterType, source, values)));
            }
            parameters.AddRange(GetValues<ParamsAllValuesAttribute>((_, parameterType) => GetValidValues(GetAllValidValues(parameterType), parameterType)));

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

        private static async ValueTask<IReadOnlyList<ParameterInstances>> GetArgumentsInstancesAsync(MethodInfo benchmark, Type benchmarkType, SummaryStyle summaryStyle, CancellationToken cancellationToken)
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
                    throw new InvalidOperationException($"Benchmark {benchmark.Name} has invalid number of defined arguments provided with [Arguments]! {argumentsAttribute.Values.Length} instead of {parameterDefinitions.Length}.");

                result.Add(
                    new(argumentsAttribute.Values
                        .Select((value, index) =>
                        {
                            var definition = parameterDefinitions[index];
                            return new ParameterInstance(definition, new ParameterValue.Constant(value, definition.ParameterType), summaryStyle);
                        })
                        .ToArray()
                    )
                );
            }

            if (!benchmark.HasAttribute<ArgumentsSourceAttribute>())
                return result;

            var argumentsSourceAttribute = benchmark.GetCustomAttribute<ArgumentsSourceAttribute>()!;
            var targetType = argumentsSourceAttribute.Type ?? benchmarkType;

            var valuesInfo = await GetValidValuesForParamsSourceAsync(targetType, argumentsSourceAttribute.Name, cancellationToken).ConfigureAwait();
            for (int sourceIndex = 0; sourceIndex < valuesInfo.values.Length; sourceIndex++)
                result.Add(SmartParamBuilder.CreateForArguments(benchmark, parameterDefinitions, valuesInfo, sourceIndex, summaryStyle));

            return result;
        }

        private static ImmutableArray<BenchmarkCase> GetFilteredBenchmarks(BenchmarkCase[] benchmarks, IEnumerable<IFilter> filters)
            => [.. benchmarks.Where(benchmark => filters.All(filter => filter.Predicate(benchmark)))];

        private static void AssertMethodHasCorrectSignature(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any() && !methodInfo.HasAttribute<ArgumentsAttribute>() && !methodInfo.HasAttribute<ArgumentsSourceAttribute>())
                throw new InvalidBenchmarkDeclarationException($"{methodType} method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        private static void AssertMethodIsAccessible(string methodType, MethodInfo methodInfo)
        {
            if (!methodInfo.IsPublic)
                throw new InvalidBenchmarkDeclarationException($"{methodType} method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public.");
            /* Moved the code that verifies if DeclaringType of a given MethodInfo (a method) is publicly accessible to CompilationValidator */
        }

        private static void AssertMethodIsNotGeneric(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new InvalidBenchmarkDeclarationException($"{methodType} method {methodInfo.Name} is generic.\nGeneric {methodType} methods are not supported.");
        }

        private static IReadOnlyList<ParameterValue> GetValidValues(object?[] values, Type parameterType)
            => [.. values.Select(value => new ParameterValue.Constant(value, parameterType))];

        private static async ValueTask<(MemberInfo source, object?[] values)> GetValidValuesForParamsSourceAsync(Type sourceType, string sourceName, CancellationToken cancellationToken)
        {
            var source = sourceType.FindSourceMember(sourceName);

            if (source == null)
                throw NoSourceMemberFound(sourceType, sourceName);

            // A source method may have parameters as long as they are all optional (e.g. an async iterator with an
            // [EnumeratorCancellation] CancellationToken); we invoke it with their default values.
            object? sourceValue = source is MethodInfo method
                ? method.Invoke(method.IsStatic ? null : Activator.CreateInstance(sourceType), GetDefaultArguments(method))
                : ((PropertyInfo) source).GetValue(((PropertyInfo) source).GetMethod!.IsStatic ? null : Activator.CreateInstance(sourceType)!);

            return (source, await ToArrayAsync(sourceValue, source, sourceType, cancellationToken).ConfigureAwait());
        }

        private static InvalidBenchmarkDeclarationException NoSourceMemberFound(Type sourceType, string sourceName)
        {
            var namedMethods = sourceType.GetAllMethods().Where(method => method.Name == sourceName && method.IsPublic).ToArray();

            if (namedMethods.Any(method => method.IsGenericMethodDefinition))
                return new InvalidBenchmarkDeclarationException($"Source method {sourceName} of type {sourceType.GetDisplayName()} is generic.\nGeneric source methods are not supported.");

            return namedMethods.Length > 0
                ? new InvalidBenchmarkDeclarationException($"{sourceType.Name}.{sourceName} has required parameters, unable to read values for [ParamsSource]/[ArgumentsSource]. A source method must be parameterless or have only optional parameters.")
                : new InvalidBenchmarkDeclarationException($"{sourceType.Name} has no public, accessible method/property called {sourceName}, unable to read values for [ParamsSource].");
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

        private static async ValueTask<object?[]> ToArrayAsync(object? sourceValue, MemberInfo memberInfo, Type type, CancellationToken cancellationToken)
        {
            var sourceType = memberInfo is MethodInfo methodInfo
                ? methodInfo.ReturnType
                : ((PropertyInfo) memberInfo).PropertyType;

            // Checked before the shape, so a null async source reports the same declaration error a null
            // synchronous source does instead of failing while being enumerated.
            if (sourceValue == null)
                throw new InvalidBenchmarkDeclarationException($"{memberInfo.Name} of type {type.Name} returned null, unable to read values for [ParamsSource]/[ArgumentsSource].");

            // Reading the values puts each into an object[], which a ref struct cannot enter - the enumeration fails
            // inside reflection saying nothing about the benchmark. Expressible since .NET 10 gave IEnumerable<T> an
            // allows-ref-struct type parameter. Asked of both shapes, and so ahead of either: an async source reads
            // its values into the same object[]. SourceReturnTypeValidator reports the declaration this substitutes.
            if (memberInfo.GetSourceReturnType().TryGetSourceElementType(out var refLikeCandidate) && refLikeCandidate.IsByRefLike())
                throw new InvalidBenchmarkDeclarationException(
                    $"{type.Name}.{memberInfo.Name} yields {refLikeCandidate.GetDisplayName()}, which is a ref struct, and BenchmarkDotNet cannot read a value into one."
                    + " Please, yield what the value is built from - IEnumerable<byte[]> for a ReadOnlySpan<byte> parameter - and let the benchmark take the ref struct.");

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

            throw new InvalidBenchmarkDeclarationException($"{memberInfo.Name} of type {type.Name} does not implement IEnumerable or IAsyncEnumerable<T>, unable to read values for [ParamsSource]");
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
