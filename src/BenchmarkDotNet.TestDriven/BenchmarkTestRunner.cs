using System;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Attributes;
using TestDriven.Framework;

namespace BenchmarkDotNet.TestDriven
{
    public class BenchmarkTestRunner : ITestRunner
    {
        Type configType;

        public BenchmarkTestRunner()
        {
        }

        public BenchmarkTestRunner(Type configType)
        {
            this.configType = configType;
        }

        public TestRunState RunMember(ITestListener testListener, Assembly assembly, MemberInfo member)
        {
            try
            {
                var summary = run(assembly, member);
                if (summary == null)
                {
                    return TestRunState.NoTests;
                }

                foreach (var benchmark in summary.Benchmarks)
                {
                    var testResult = getTestResult(benchmark);
                    testListener.TestFinished(testResult);
                }

                testListener.TestResultsUrl(summary.ResultsDirectoryPath);

                return TestRunState.Success;
            }
            catch(TestRunnerException e)
            {
                testListener.WriteLine(e.Message, Category.Warning);
                return TestRunState.Error;
            }
        }

        static TestResult getTestResult(Benchmark benchmark)
        {
            var testResult = new TestResult();
            testResult.TestRunnerName = getTestRunnerName();
            testResult.Name = getTestName(benchmark.Target.Method);
            testResult.Method = benchmark.Target.Method;
            return testResult;
        }

        static string getTestName(MethodInfo method)
        {
            return method.DeclaringType.FullName + "." + method.Name;
        }

        static string getTestRunnerName()
        {
            var version = typeof(BenchmarkAttribute).GetTypeInfo().Assembly.GetName().Version;
            return string.Format("BenchmarkDotNet {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        Summary run(Assembly assembly, MemberInfo member)
        {
            if (member is TypeInfo)
            {
                var type = (TypeInfo)member;
                if (isBenchmarkType(type))
                {
                    var config = getConfig(assembly);
                    return BenchmarkRunner.Run(type.AsType(), config);
                }
            }
            else if (member is MethodInfo)
            {
                var method = (MethodInfo)member;
                if (isBenchmarkMethod(method))
                {
                    var type = method.DeclaringType;
                    var config = getConfig(assembly);
                    return BenchmarkRunner.Run(type, new[] { method }, config);
                }
            }

            return null;
        }

        IConfig getConfig(Assembly assembly)
        {
            var configType = getConfigType(assembly);
            try
            {
                var config = (IConfig)Activator.CreateInstance(configType);
                return config;
            }
            catch(InvalidCastException)
            {
                throw new TestRunnerException(string.Format("ConfigType `{0}` must implement `{1}`.",
                    configType.FullName, typeof(IConfig).FullName));
            }
        }

        Type getConfigType(Assembly assembly)
        {
            if (configType != null)
            {
                return configType;
            }

            var attribute = assembly.GetCustomAttribute<BenchmarkTestRunnerAttribute>();
            return attribute.ConfigType;
        }

        static bool isBenchmarkType(TypeInfo type)
        {
            foreach (var method in type.GetMethods())
            {
                if (isBenchmarkMethod(method))
                {
                    return true;
                }
            }

            return false;
        }

        static bool isBenchmarkMethod(MethodInfo method)
        {
            return method.GetCustomAttribute<BenchmarkAttribute>() != null;
        }

        public TestRunState RunNamespace(ITestListener testListener, Assembly assembly, string ns)
        {
            return TestRunState.NoTests;
        }

        public TestRunState RunAssembly(ITestListener testListener, Assembly assembly)
        {
            return TestRunState.NoTests;
        }

        class TestRunnerException : Exception
        {
            internal TestRunnerException(string message) : base(message)
            {
            }
        }
    }
}
