﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Running
{
    public static partial class BenchmarkConverter
    {
        public static Benchmark[] TypeToBenchmarks(Type type, IConfig config = null)
        {
            config = GetFullConfig(type, config);

            var allMethods = type.GetMethods();
            return MethodsToBenchmarks(type, allMethods, config);
        }

        public static Benchmark[] MethodsToBenchmarks(Type containingType, MethodInfo[] methods, IConfig config = null)
        {
            var globalSetupMethod = GetWrappingMethod<GlobalSetupAttribute>(methods, "GlobalSetup");
            var targetMethods = methods.Where(method => method.HasAttribute<BenchmarkAttribute>()).ToArray();
            var globalCleanupMethod = GetWrappingMethod<GlobalCleanupAttribute>(methods, "GlobalCleanup");
            var iterationSetupMethod = GetWrappingMethod<IterationSetupAttribute>(methods, "IterationSetup");
            var iterationCleanupMethod = GetWrappingMethod<IterationCleanupAttribute>(methods, "IterationCleanup");

            var parameterDefinitions = GetParameterDefinitions(containingType);
            var parameterInstancesList = parameterDefinitions.Expand();

            var rawJobs = config?.GetJobs().ToArray() ?? Array.Empty<Job>();
            if (rawJobs.IsEmpty())
                rawJobs = new[] { Job.Default };
            var jobs = rawJobs.Distinct().ToArray();

            var targets = GetTargets(targetMethods, containingType, globalSetupMethod, globalCleanupMethod, iterationSetupMethod, iterationCleanupMethod).ToArray();

            var benchmarks = (
                from target in targets
                from job in jobs
                from parameterInstance in parameterInstancesList
                select new Benchmark(target, job, parameterInstance)).ToArray();
            
            var filters = config.GetFilters().ToList();
            benchmarks = GetFilteredBenchmarks(benchmarks, filters);

            var orderProvider = config?.GetOrderProvider() ?? DefaultOrderProvider.Instance;
            return orderProvider.GetExecutionOrder(benchmarks).ToArray();
        }

        public static IConfig GetFullConfig(Type type, IConfig config)
        {
            config = config ?? DefaultConfig.Instance;
            if (type != null)
            {
                var typeAttributes = type.GetTypeInfo().GetCustomAttributes(true).OfType<IConfigSource>();
                var assemblyAttributes = type.GetTypeInfo().Assembly.GetCustomAttributes().OfType<IConfigSource>();
                var allAttributes = typeAttributes.Concat(assemblyAttributes);
                foreach (var configSource in allAttributes)
                    config = ManualConfig.Union(config, configSource.Config);
            }
            return config;
        }

        private static IEnumerable<Target> GetTargets(MethodInfo[] targetMethods, Type type, MethodInfo globalSetupMethod, MethodInfo globalCleanupMethod, 
            MethodInfo iterationSetupMethod, MethodInfo iterationCleanupMethod)
        {
            return targetMethods
                .Where(m => m.HasAttribute<BenchmarkAttribute>())
                .Select(methodInfo => CreateTarget(type, globalSetupMethod, methodInfo, globalCleanupMethod, iterationSetupMethod, iterationCleanupMethod,
                    methodInfo.ResolveAttribute<BenchmarkAttribute>(), targetMethods));
        }

        private static Target CreateTarget(
            Type type, 
            MethodInfo globalSetupMethod, 
            MethodInfo methodInfo, 
            MethodInfo globalCleanupMethod,
            MethodInfo iterationSetupMethod,
            MethodInfo iterationCleanupMethod,
            BenchmarkAttribute attr,            
            MethodInfo[] targetMethods)
        {
            var target = new Target(
                type,
                methodInfo,
                globalSetupMethod,
                globalCleanupMethod,
                iterationSetupMethod,
                iterationCleanupMethod,
                attr.Description,
                baseline: attr.Baseline,
                categories: GetCategories(methodInfo),
                operationsPerInvoke: attr.OperationsPerInvoke, 
                methodIndex: Array.IndexOf(targetMethods, methodInfo));
            AssertMethodHasCorrectSignature("Benchmark", methodInfo);
            AssertMethodIsAccessible("Benchmark", methodInfo);
            AssertMethodIsNotGeneric("Benchmark", methodInfo);
            return target;
        }

        private static ParameterDefinitions GetParameterDefinitions(Type type)
        {
            var reflectionFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var allFields = type.GetFields(reflectionFlags).Select(f => new
            {
                f.Name,
                Attribute = f.ResolveAttribute<ParamsAttribute>(),
                Private = f.IsPrivate,
                IsStatic = f.IsStatic,
                ParameterType = f.FieldType
            });
            var allProperties = type.GetProperties(reflectionFlags).Select(p => new
            {
                p.Name,
                Attribute = p.ResolveAttribute<ParamsAttribute>(),
                Private = p.GetSetMethod() == null,
                IsStatic = p.GetSetMethod() != null && p.GetSetMethod().IsStatic,
                ParameterType = p.PropertyType
            });
            var allParameterMembers = allFields.Concat(allProperties).Where(member => member.Attribute != null).ToArray();

            var firstPrivateMember = allParameterMembers.FirstOrDefault(member => member.Private);
            if (firstPrivateMember != null)
                throw new InvalidOperationException($"Member \"{firstPrivateMember.Name}\" must be public if it has the [Params(..)] attribute applied to it");

            var definitions = allParameterMembers
                .Select(member => 
                    new ParameterDefinition(
                        member.Name, 
                        member.IsStatic, 
                        GetValidValues(member.Attribute.Values, member.ParameterType)))
                .ToArray();

            return new ParameterDefinitions(definitions);
        }

        private static MethodInfo GetWrappingMethod<T>(MethodInfo[] methods, string methodName) where T : Attribute
        {
            var globalSetupMethod = methods.FirstOrDefault(m => m.HasAttribute<T>());
            if (globalSetupMethod != null)
            {
                // "GlobalSetup" or "GlobalCleanup" methods are optional, but if they're there, they must have the correct signature, accessibility, etc ...
                AssertMethodHasCorrectSignature(methodName, globalSetupMethod);
                AssertMethodIsAccessible(methodName, globalSetupMethod);
                AssertMethodIsNotGeneric(methodName, globalSetupMethod);
            }
            return globalSetupMethod;
        }

        private static string[] GetCategories(MethodInfo method)
        {
            var attributes = new List<BenchmarkCategoryAttribute>();
            attributes.AddRange(method.GetCustomAttributes(typeof(BenchmarkCategoryAttribute), false).OfType<BenchmarkCategoryAttribute>());
            var type = method.DeclaringType;
            if (type != null)
            {
                attributes.AddRange(type.GetTypeInfo().GetCustomAttributes(typeof(BenchmarkCategoryAttribute), false).OfType<BenchmarkCategoryAttribute>());
                attributes.AddRange(type.GetTypeInfo().Assembly.GetCustomAttributes().OfType<BenchmarkCategoryAttribute>());
            }
            if (attributes.Count == 0)
                return Array.Empty<string>();
            return attributes.SelectMany(attr => attr.Categories).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static Benchmark[] GetFilteredBenchmarks(IList<Benchmark> benchmarks, IList<IFilter> filters)
        {
            return benchmarks.Where(benchmark => filters.All(filter => filter.Predicate(benchmark))).ToArray();
        }

        private static void AssertMethodHasCorrectSignature(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any())
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        private static void AssertMethodIsAccessible(string methodType, MethodInfo methodInfo)
        {
            if (!methodInfo.IsPublic)
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public.");

            var declaringType = methodInfo.DeclaringType;

            while (declaringType != null)
            {
                if (!declaringType.GetTypeInfo().IsPublic && !declaringType.GetTypeInfo().IsNestedPublic)
                    throw new InvalidOperationException($"{declaringType.FullName} containing {methodType} method {methodInfo.Name} has incorrect access modifiers.\nDeclaring type must be public.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertMethodIsNotGeneric(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} is generic.\nGeneric {methodType} methods are not supported.");
        }

        private static object[] GetValidValues(object[] values, Type parameterType)
        {
            if (values == null && parameterType.IsNullable())
            {
                return new object[] { null };
            }

            return values;
        }
    }
}
