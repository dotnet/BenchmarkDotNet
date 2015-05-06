using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkTask
    {
        public int RunCount { get; }
        public BenchmarkConfiguration Configuration { get; }
        public BenchmarkSettings Settings { get; }

        public BenchmarkTask(int runCount, BenchmarkConfiguration configuration, BenchmarkSettings settings)
        {
            RunCount = runCount;
            Configuration = configuration;
            Settings = settings;
        }

        public static IEnumerable<BenchmarkTask> Resolve(MethodInfo methodInfo, BenchmarkSettings defaultSettings)
        {
            var attrs = methodInfo.GetCustomAttributes(typeof(TaskAttribute), false).Cast<TaskAttribute>().ToList();
            if (attrs.Count == 0)
                attrs = methodInfo.DeclaringType.GetCustomAttributes(typeof(TaskAttribute), false).Cast<TaskAttribute>().ToList();
            if (attrs.Count == 0)
                attrs.Add(new TaskAttribute(warmupIterationCount: defaultSettings.WarmupIterationCount, targetIterationCount: defaultSettings.TargetIterationCount));
            return attrs.Select(attr => attr.Task);
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(RunCount), RunCount.ToString());
                foreach (var property in Configuration.Properties)
                    yield return property;
                foreach (var property in Settings.Properties)
                    yield return property;
            }
        }
    }
}