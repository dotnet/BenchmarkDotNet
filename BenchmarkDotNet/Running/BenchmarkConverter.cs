using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using Microsoft.CSharp;

namespace BenchmarkDotNet.Running
{
    internal static class BenchmarkConverter
    {
        public static IList<Benchmark> TypeToBenchmarks(Type type, IConfig config = null)
        {
            config = GetFullConfig(type, config);

            var allMethods = type.GetMethods();

            var setupMethod = GetSetupMethod(allMethods);
            var targetMethods = allMethods.Where(method => method.HasAttribute<BenchmarkAttribute>()).ToArray();

            var parameterDefinitions = GetParameterDefinitions(type);
            var parameterInstancesList = parameterDefinitions.Expand();

            var rawJobs = config?.GetJobs().ToArray() ?? new IJob[0];
            if (rawJobs.IsEmpty())
                rawJobs = new[] { Job.Default };
            var jobs = rawJobs.ToArray();

            var targets = GetTargets(targetMethods, type, setupMethod).ToArray();

            return (
                from target in targets
                from job in jobs
                from parameterInstancese in parameterInstancesList
                select new Benchmark(target, job, parameterInstancese)).ToSortedList();
        }

        public static IConfig GetFullConfig(Type type, IConfig config)
        {
            config = config ?? DefaultConfig.Instance;
            var configAttribute = type?.ResolveAttribute<ConfigAttribute>();
            if (configAttribute != null)
                config = ManualConfig.Union(config, configAttribute.Config);
            return config;
        }

        private static IEnumerable<Target> GetTargets(IEnumerable<MethodInfo> targetMethods, Type type, MethodInfo setupMethod) => targetMethods.
            Where(m => m.HasAttribute<BenchmarkAttribute>()).
            Select(methodInfo => CreateTarget(type, setupMethod, methodInfo, methodInfo.ResolveAttribute<BenchmarkAttribute>()));

        private static Target CreateTarget(Type type, MethodInfo setupMethod, MethodInfo methodInfo, BenchmarkAttribute attr)
        {
            var target = new Target(type, methodInfo, setupMethod, attr.Description, baseline: attr.Baseline, operationsPerInvoke: attr.OperationsPerInvoke);
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
                IsStatic = f.IsStatic
            });
            var allProperties = type.GetProperties(reflectionFlags).Select(p => new
            {
                p.Name,
                Attribute = p.ResolveAttribute<ParamsAttribute>(),
                Private = p.GetSetMethod() == null,
                IsStatic = p.GetSetMethod() != null && p.GetSetMethod().IsStatic
            });
            var allParameterMembers = allFields.Concat(allProperties).Where(member => member.Attribute != null).ToArray();

            var firstPrivateMember = allParameterMembers.FirstOrDefault(member => member.Private);
            if (firstPrivateMember != null)
                throw new InvalidOperationException($"Member \"{firstPrivateMember.Name}\" must be public if it has the [Params(..)] attribute applied to it");

            var definitions = allParameterMembers.Select(member => new ParameterDefinition(member.Name, member.IsStatic, member.Attribute.Values)).ToArray();
            return new ParameterDefinitions(definitions);
        }

        private static MethodInfo GetSetupMethod(MethodInfo[] methods)
        {
            var setupMethod = methods.FirstOrDefault(m => m.HasAttribute<SetupAttribute>());
            if (setupMethod != null)
            {
                // setupMethod is optional, but if it's there it must have the correct signature, accessibility, etc
                AssertMethodHasCorrectSignature("Setup", setupMethod);
                AssertMethodIsAccessible("Setup", setupMethod);
                AssertMethodIsNotGeneric("Setup", setupMethod);
            }
            return setupMethod;
        }

        public static IList<Benchmark> UrlToBenchmarks(string url, IConfig config = null)
        {
            string benchmarkContent;
            try
            {
                var webRequest = WebRequest.Create(url);
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                    benchmarkContent = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(benchmarkContent))
                {
                    Console.WriteLine($"content of '{url}' is empty.");
                    return new Benchmark[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("BuildException: " + e.Message);
                return new Benchmark[0];
            }
            return SourceToBenchmarks(benchmarkContent, config);
        }

        public static IList<Benchmark> SourceToBenchmarks(string source, IConfig config = null)
        {
            string benchmarkContent = source;
            var cSharpCodeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters(new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }) { CompilerOptions = "/unsafe" };
            compilerParameters.ReferencedAssemblies.Add(typeof(BenchmarkRunner).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText));
                return new Benchmark[0];
            }
            return (
                from type in compilerResults.CompiledAssembly.GetTypes()
                from benchmark in TypeToBenchmarks(type, config)
                let target = benchmark.Target
                select new Benchmark(
                    new Target(target.Type, target.Method, target.SetupMethod, target.MethodTitle, benchmarkContent, target.Baseline, target.OperationsPerInvoke),
                    benchmark.Job,
                    benchmark.Parameters)).ToList();
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
                if (!declaringType.IsPublic && !declaringType.IsNestedPublic)
                    throw new InvalidOperationException($"{methodType} method {methodInfo.Name} defined within type {declaringType.FullName} has incorrect access modifiers.\nDeclaring type must be public.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertMethodIsNotGeneric(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} is generic.\nGeneric {methodType} methods are not supported.");
        }
    }
}