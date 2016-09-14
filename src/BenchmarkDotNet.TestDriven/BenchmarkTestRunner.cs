using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Attributes;
using TestDriven.Framework;

namespace BenchmarkDotNet.TestDriven
{
    public abstract class BenchmarkTestRunner : ITestRunner
    {
        public abstract IConfig GetConfig();

        public TestRunState RunMember(ITestListener testListener, Assembly assembly, MemberInfo member)
        {
            var summary = run(member);
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

        Summary run(MemberInfo member)
        {
            if (member is TypeInfo)
            {
                var type = (TypeInfo)member;
                if (isBenchmarkType(type))
                {
                    return BenchmarkRunner.Run(type.AsType(), GetConfig());
                }
            }
            else if (member is MethodInfo)
            {
                var method = (MethodInfo)member;
                if (isBenchmarkMethod(method))
                {
                    var type = method.DeclaringType;
                    return BenchmarkRunner.Run(type, new[] { method }, GetConfig());
                }
            }

            return null;
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
    }
}
