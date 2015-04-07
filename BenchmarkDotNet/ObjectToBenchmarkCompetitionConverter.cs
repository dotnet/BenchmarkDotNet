using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet
{
    internal static class ObjectToBenchmarkCompetitionConverter
    {
        public static IEnumerable<IBenchmark> Convert(object competition)
        {
            var methods = competition.GetType().GetMethods();
            for (int i = 0; i < methods.Length; i++)
            {
                var methodInfo = methods[i];
                var benchmarkMethodAttribute = GetBenchmarkMethodAttribute(methodInfo);
                if (benchmarkMethodAttribute != null)
                {
                    AssertBenchmarkMethodHasCorrectSignature(methodInfo);
                    var name = benchmarkMethodAttribute.Name ?? methodInfo.Name;
                    Action action = () => methodInfo.Invoke(competition, new object[0]);

                    Action initialize = null;
                    var methodInfoInitialize = GetBenchmarkMethodInitialize(competition, methodInfo.Name);
                    if (methodInfoInitialize != null)
                    {
                        AssertBenchmarkMethodHasCorrectSignature(methodInfoInitialize);
                        initialize = () => methodInfoInitialize.Invoke(competition, new object[0]);
                    }

                    Action clean = null;
                    var methodInfoClean = GetBenchmarkMethodClean(competition, name);
                    if (methodInfoClean != null)
                    {
                        AssertBenchmarkMethodHasCorrectSignature(methodInfoClean);
                        clean = () => methodInfoClean.Invoke(competition, new object[0]);
                    }

                    yield return new Benchmark(name, initialize, action, clean);
                }
            }
        }

        private static void AssertBenchmarkMethodHasCorrectSignature(MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any())
                throw new InvalidOperationException($"Benchmark method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        private static BenchmarkMethodAttribute GetBenchmarkMethodAttribute(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(BenchmarkMethodAttribute), false).FirstOrDefault() as BenchmarkMethodAttribute;
        }

        private static MethodInfo GetBenchmarkMethodInitialize(object competition, string name)
        {
            return (
                from methodInfo in competition.GetType().GetMethods()
                let attribute = methodInfo.GetCustomAttributes(typeof(BenchmarkMethodInitializeAttribute), false).
                                OfType<BenchmarkMethodInitializeAttribute>().FirstOrDefault()
                where attribute != null && (attribute.Name == name || methodInfo.Name == name + "Initialize")
                select methodInfo).FirstOrDefault();
        }

        private static MethodInfo GetBenchmarkMethodClean(object competition, string name)
        {
            return (
                from methodInfo in competition.GetType().GetMethods()
                let attribute = methodInfo.GetCustomAttributes(typeof(BenchmarkMethodCleanAttribute), false).
                                OfType<BenchmarkMethodCleanAttribute>().FirstOrDefault()
                where attribute != null && (attribute.Name == name || methodInfo.Name == name + "Clean")
                select methodInfo).FirstOrDefault();
        }
    }
}