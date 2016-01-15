using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tasks;
using Microsoft.CSharp;

namespace BenchmarkDotNet
{
    internal static class BenchmarkConverter
    {
        public static IEnumerable<Benchmark> TypeToBenchmarks(Type type)
        {
            var methods = type.GetMethods();
            var setupMethod = methods.FirstOrDefault(m => m.ResolveAttribute<SetupAttribute>() != null);
            if (setupMethod != null)
            {
                // setupMethod is optional, but if it's there it must have the correct signature, accessibility, etc
                AssertMethodHasCorrectSignature("Setup", setupMethod);
                AssertMethodIsAccessible("Setup", setupMethod);
                AssertMethodIsNotGeneric("Setup", setupMethod);
            }

            // If there is one, get the single Field or Property that has the [Params(..)] attribute
            var reflectionFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = type.GetFields(reflectionFlags).Select(f => new
            {
                f.Name,
                Attribute = f.ResolveAttribute<ParamsAttribute>(),
                Private = f.IsPrivate,
                IsStatic = f.IsStatic,
            });
            var properties = type.GetProperties(reflectionFlags).Select(p => new
            {
                p.Name,
                Attribute = p.ResolveAttribute<ParamsAttribute>(),
                Private = p.GetSetMethod() == null ? true : false,
                IsStatic = p.GetSetMethod() != null ? p.GetSetMethod().IsStatic : false
            });
            var fieldOrProperty = fields.Concat(properties).FirstOrDefault(i => i.Attribute != null);

            var privateField = fields.FirstOrDefault(f => f.Attribute != null && f.Private);
            if (privateField != null)
                throw new InvalidOperationException($"Field \"{privateField.Name}\" must be public if it has the [Params(..)] attribute applied to it");

            var privateProperty = properties.FirstOrDefault(p => p.Attribute != null && p.Private);
            if (privateProperty != null)
                throw new InvalidOperationException($"Property \"{privateProperty.Name}\" must be public and writable if it has the [Params(..)] attribute applied to it");

            for (int i = 0; i < methods.Length; i++)
            {
                var methodInfo = methods[i];
                var benchmarkAttribute = methodInfo.ResolveAttribute<BenchmarkAttribute>();
                if (benchmarkAttribute != null)
                {
                    var target = new BenchmarkTarget(type, methodInfo, setupMethod, benchmarkAttribute.Description, baseline: benchmarkAttribute.Baseline);
                    AssertMethodHasCorrectSignature("Benchmark", methodInfo);
                    AssertMethodIsAccessible("Benchmark", methodInfo);
                    AssertMethodIsNotGeneric("Benchmark", methodInfo);
                    foreach (var task in BenchmarkTask.Resolve(methodInfo))
                    {
                        if (fieldOrProperty == null)
                        {
                            yield return new Benchmark(target, task);
                        }
                        else
                        {
                            var parametersSets = new BenchmarkParametersSets(fieldOrProperty.Name, fieldOrProperty.IsStatic, fieldOrProperty.Attribute.Args);
                            // All the properties of BenchmarkTask and it's children are immutable, so cloning a BenchmarkTask like this should be safe
                            var newTask = new BenchmarkTask(task.ProcessCount, task.Configuration, parametersSets);
                            yield return new Benchmark(target, newTask);
                        }
                    }
                }
            }
        }

        public static IEnumerable<Benchmark> UrlToBenchmarks(string url)
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
            return SourceToBenchmarks(benchmarkContent);
        }

        public static IEnumerable<Benchmark> SourceToBenchmarks(string source)
        {
            string benchmarkContent = source;
            var cSharpCodeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters(new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }) { CompilerOptions = "/unsafe" };
            compilerParameters.ReferencedAssemblies.Add(typeof(BenchmarkRunner).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText));
                yield break;
            }
            foreach (var type in compilerResults.CompiledAssembly.GetTypes())
                foreach (var benchmark in TypeToBenchmarks(type))
                {
                    yield return new Benchmark(new BenchmarkTarget(benchmark.Target.Type,
                                                                   benchmark.Target.Method,
                                                                   benchmark.Target.SetupMethod,
                                                                   benchmark.Target.Description,
                                                                   benchmarkContent,
                                                                   benchmark.Target.Baseline),
                                               benchmark.Task);
                }
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