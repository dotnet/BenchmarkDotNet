using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Running
{
    public static partial class BenchmarkConverter
    {
        private const BindingFlags AllMethodsFlags =  BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static BenchmarkRunInfo TypeToBenchmarks(Type type, IConfig? config = null)
        {
            if (type.IsGenericTypeDefinition)
                throw new InvalidBenchmarkDeclarationException($"{type.Name} is generic type definition, use BenchmarkSwitcher for it"); // for "open generic types" should be used BenchmarkSwitcher

            // We should check all methods including private to notify users about private methods with the [Benchmark] attribute
            var benchmarkMethods = GetOrderedBenchmarkMethods(type.GetMethods(AllMethodsFlags));

            return MethodsToBenchmarksWithFullConfig(type, benchmarkMethods, config);
        }

        public static BenchmarkRunInfo MethodsToBenchmarks(Type containingType, MethodInfo[] benchmarkMethods, IConfig? config = null)
            => MethodsToBenchmarksWithFullConfig(containingType, GetOrderedBenchmarkMethods(benchmarkMethods), config);

        private static MethodInfo[] GetOrderedBenchmarkMethods(MethodInfo[] methods)
            => methods
                .Select(method => (method, attribute: method.ResolveAttribute<BenchmarkAttribute>()))
                .Where(pair => pair.attribute is not null)
                .OrderBy(pair => pair.attribute.SourceCodeFile)
                .ThenBy(pair => pair.attribute.SourceCodeLineNumber)
                .Select(pair => pair.method)
                .ToArray();

        private static BenchmarkRunInfo MethodsToBenchmarksWithFullConfig(Type type, MethodInfo[] benchmarkMethods, IConfig? config)
        {
            var allMethods = type.GetMethods(AllMethodsFlags); // benchmarkMethods can be filtered, without Setups, look #564
            var configPerType = GetFullTypeConfig(type, config);

            var globalSetupMethods = GetAttributedMethods<GlobalSetupAttribute>(allMethods, "GlobalSetup");
            var globalCleanupMethods = GetAttributedMethods<GlobalCleanupAttribute>(allMethods, "GlobalCleanup");
            var iterationSetupMethods = GetAttributedMethods<IterationSetupAttribute>(allMethods, "IterationSetup");
            var iterationCleanupMethods = GetAttributedMethods<IterationCleanupAttribute>(allMethods, "IterationCleanup");

            var targets = GetTargets(benchmarkMethods, type, globalSetupMethods, globalCleanupMethods, iterationSetupMethods, iterationCleanupMethods,
                configPerType).ToArray();

            var parameterDefinitions = GetParameterDefinitions(type);
            var parameterInstancesList = parameterDefinitions.Expand(configPerType.SummaryStyle);

            var benchmarks = new List<BenchmarkCase>();

            foreach (var target in targets)
            {
                var argumentsDefinitions = GetArgumentsDefinitions(target.WorkloadMethod, target.Type, configPerType.SummaryStyle).ToArray();

                var parameterInstances =
                    (from parameterInstance in parameterInstancesList
                     from argumentDefinition in argumentsDefinitions
                     select new ParameterInstances(parameterInstance.Items.Concat(argumentDefinition.Items).ToArray())).ToArray();

                var configPerMethod = GetFullMethodConfig(target.WorkloadMethod, configPerType);

                var benchmarksForTarget =
                    from job in configPerMethod.GetJobs()
                    from parameterInstance in parameterInstances
                    select BenchmarkCase.Create(target, job, parameterInstance, configPerMethod);

                benchmarks.AddRange(GetFilteredBenchmarks(benchmarksForTarget, configPerMethod.GetFilters()));
            }

            var orderedBenchmarks = configPerType.Orderer.GetExecutionOrder(benchmarks.ToImmutableArray()).ToArray();

            return new BenchmarkRunInfo(orderedBenchmarks, type, configPerType);
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
                                                   methodInfo.ResolveAttribute<BenchmarkAttribute>(),
                                                   targetMethods,
                                                   config));
        }

        private static MethodInfo GetTargetedMatchingMethod(MethodInfo benchmarkMethod, Tuple<MethodInfo, TargetedAttribute>[] methods)
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
            MethodInfo globalSetupMethod,
            MethodInfo methodInfo,
            MethodInfo globalCleanupMethod,
            MethodInfo iterationSetupMethod,
            MethodInfo iterationCleanupMethod,
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

        private static ParameterDefinitions GetParameterDefinitions(Type type)
        {
            IEnumerable<ParameterDefinition> GetDefinitions<TAttribute>(Func<TAttribute, Type, object[]> getValidValues) where TAttribute : PriorityAttribute
            {
                const BindingFlags reflectionFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var allMembers = type.GetTypeMembersWithGivenAttribute<TAttribute>(reflectionFlags);
                return allMembers.Select(member =>
                    new ParameterDefinition(
                        member.Name,
                        member.IsStatic,
                        getValidValues(member.Attribute, member.ParameterType),
                        false,
                        member.ParameterType,
                        member.Attribute.Priority));
            }

            var paramsDefinitions = GetDefinitions<ParamsAttribute>((attribute, parameterType) => GetValidValues(attribute.Values, parameterType));

            var paramsSourceDefinitions = GetDefinitions<ParamsSourceAttribute>((attribute, parameterType) =>
            {
                var paramsValues = GetValidValuesForParamsSource(type, attribute.Name);
                return SmartParamBuilder.CreateForParams(parameterType, paramsValues.source, paramsValues.values);
            });

            var paramsAllValuesDefinitions = GetDefinitions<ParamsAllValuesAttribute>((_, parameterType) => GetAllValidValues(parameterType));

            var definitions = paramsDefinitions.Concat(paramsSourceDefinitions).Concat(paramsAllValuesDefinitions).ToArray();
            return new ParameterDefinitions(definitions);
        }

        private static IEnumerable<ParameterInstances> GetArgumentsDefinitions(MethodInfo benchmark, Type target, SummaryStyle summaryStyle)
        {
            var argumentsAttributes = benchmark.GetCustomAttributes<PriorityAttribute>();
            int priority = argumentsAttributes.Select(attribute => attribute.Priority).Sum();

            var parameterDefinitions = benchmark.GetParameters()
                .Select(parameter => new ParameterDefinition(parameter.Name, false, Array.Empty<object>(), true, parameter.ParameterType, priority))
                .ToArray();

            if (parameterDefinitions.IsEmpty())
            {
                yield return new ParameterInstances(Array.Empty<ParameterInstance>());
                yield break;
            }

            foreach (var argumentsAttribute in benchmark.GetCustomAttributes<ArgumentsAttribute>())
            {
                if (parameterDefinitions.Length != argumentsAttribute.Values.Length)
                    throw new InvalidOperationException($"Benchmark {benchmark.Name} has invalid number of defined arguments provided with [Arguments]! {argumentsAttribute.Values.Length} instead of {parameterDefinitions.Length}.");

                yield return new ParameterInstances(
                    argumentsAttribute
                        .Values
                        .Select((value, index) =>
                            {
                                var definition = parameterDefinitions[index];
                                var type = definition.ParameterType;
                                return new ParameterInstance(definition, Map(value, type), summaryStyle);
                            })
                        .ToArray());
            }

            if (!benchmark.HasAttribute<ArgumentsSourceAttribute>())
                yield break;

            var argumentsSourceAttribute = benchmark.GetCustomAttribute<ArgumentsSourceAttribute>();

            var valuesInfo = GetValidValuesForParamsSource(target, argumentsSourceAttribute.Name);
            for (int sourceIndex = 0; sourceIndex < valuesInfo.values.Length; sourceIndex++)
                yield return SmartParamBuilder.CreateForArguments(benchmark, parameterDefinitions, valuesInfo, sourceIndex, summaryStyle);
        }

        private static ImmutableArray<BenchmarkCase> GetFilteredBenchmarks(IEnumerable<BenchmarkCase> benchmarks, IEnumerable<IFilter> filters)
            => benchmarks.Where(benchmark => filters.All(filter => filter.Predicate(benchmark))).ToImmutableArray();

        private static void AssertMethodHasCorrectSignature(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any() && !methodInfo.HasAttribute<ArgumentsAttribute>() && !methodInfo.HasAttribute<ArgumentsSourceAttribute>())
                throw new InvalidBenchmarkDeclarationException($"{methodType} method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        private static void AssertMethodIsAccessible(string methodType, MethodInfo methodInfo)
        {
            if (!methodInfo.IsPublic)
                throw new InvalidBenchmarkDeclarationException($"{methodType} method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public.");

            var declaringType = methodInfo.DeclaringType;

            while (declaringType != null)
            {
                if (!declaringType.GetTypeInfo().IsPublic && !declaringType.GetTypeInfo().IsNestedPublic)
                    throw new InvalidBenchmarkDeclarationException($"{declaringType.FullName} containing {methodType} method {methodInfo.Name} has incorrect access modifiers.\nDeclaring type must be public.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertMethodIsNotGeneric(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new InvalidBenchmarkDeclarationException($"{methodType} method {methodInfo.Name} is generic.\nGeneric {methodType} methods are not supported.");
        }

        private static object[] GetValidValues(object[] values, Type parameterType)
        {
            if (values == null && parameterType.IsNullable())
            {
                return new object[] { null };
            }

            return values?.Select(value => Map(value, parameterType)).ToArray();
        }

        private static object Map(object providedValue, Type type)
        {
            if (providedValue == null)
                return null;

            if (providedValue.GetType().IsArray)
            {
                return ArrayParam<IParam>.FromObject(providedValue);
            }
            // Usually providedValue contains all needed type information,
            // but in case of F# enum types in attributes are erased.
            // We can to restore them from types of arguments and fields.
            // See also:
            // https://github.com/dotnet/fsharp/issues/995
            else if (providedValue.GetType().IsEnum || type.IsEnum)
            {
                return EnumParam.FromObject(providedValue, type);
            }
            return providedValue;
        }

        private static (MemberInfo source, object[] values) GetValidValuesForParamsSource(Type parentType, string sourceName)
        {
            var paramsSourceMethod = parentType.GetAllMethods().SingleOrDefault(method => method.Name == sourceName && method.IsPublic);

            if (paramsSourceMethod != default)
                return (paramsSourceMethod, ToArray(
                    paramsSourceMethod.Invoke(paramsSourceMethod.IsStatic ? null : Activator.CreateInstance(parentType), null),
                    paramsSourceMethod,
                    parentType));

            var paramsSourceProperty = parentType.GetAllProperties().SingleOrDefault(property => property.Name == sourceName && property.GetMethod.IsPublic);

            if (paramsSourceProperty != default)
                return (paramsSourceProperty, ToArray(
                    paramsSourceProperty.GetValue(paramsSourceProperty.GetMethod.IsStatic ? null : Activator.CreateInstance(parentType)),
                    paramsSourceProperty,
                    parentType));

            throw new InvalidBenchmarkDeclarationException($"{parentType.Name} has no public, accessible method/property called {sourceName}, unable to read values for [ParamsSource]");
        }

        private static object[] ToArray(object sourceValue, MemberInfo memberInfo, Type type)
        {
            if (!(sourceValue is IEnumerable collection))
                throw new InvalidBenchmarkDeclarationException($"{memberInfo.Name} of type {type.Name} does not implement IEnumerable, unable to read values for [ParamsSource]");

            return collection.Cast<object>().ToArray();
        }

        private static object[] GetAllValidValues(Type parameterType)
        {
            if (parameterType == typeof(bool))
                return new object[] { false, true };

            if (parameterType.GetTypeInfo().IsEnum)
            {
                if (parameterType.GetTypeInfo().IsDefined(typeof(FlagsAttribute)))
                    return new object[] { Activator.CreateInstance(parameterType) };

                return Enum.GetValues(parameterType).Cast<object>().ToArray();
            }

            var nullableUnderlyingType = Nullable.GetUnderlyingType(parameterType);
            if (nullableUnderlyingType != null)
                return new object[] { null }.Concat(GetAllValidValues(nullableUnderlyingType)).ToArray();

            return new object[] { Activator.CreateInstance(parameterType) };
        }
    }
}
