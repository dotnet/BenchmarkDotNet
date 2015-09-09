using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkTask
    {
        public int ProcessCount { get; }
        public BenchmarkConfiguration Configuration { get; }
        public BenchmarkSettings Settings { get; }
        public BenchmarkParams Params { get; }

        public string Caption => Configuration.Caption + (Params != null ? Params.Caption : "");
        public string Description => Configuration.Caption + (Params != null ? Params.Caption : "");

        public BenchmarkTask(int processCount, BenchmarkConfiguration configuration, BenchmarkSettings settings, BenchmarkParams @params = null)
        {
            ProcessCount = processCount;
            Configuration = configuration;
            Settings = settings;
            Params = @params;
        }

        public static IEnumerable<BenchmarkTask> Resolve(MethodInfo methodInfo, BenchmarkSettings defaultSettings)
        {
            var attrs = methodInfo.GetCustomAttributes(typeof(BenchmarkTaskAttribute), false).Cast<BenchmarkTaskAttribute>().ToList();
            if (attrs.Count == 0)
                attrs = methodInfo.DeclaringType.GetCustomAttributes(typeof(BenchmarkTaskAttribute), false).Cast<BenchmarkTaskAttribute>().ToList();
            if (attrs.Count == 0)
                attrs.Add(new BenchmarkTaskAttribute(warmupIterationCount: defaultSettings.WarmupIterationCount, targetIterationCount: defaultSettings.TargetIterationCount));
            return attrs.Select(attr => attr.Task);
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(ProcessCount), ProcessCount.ToString());
                foreach (var property in Configuration.Properties)
                    yield return property;
                foreach (var property in Settings.Properties)
                    yield return property;
            }
        }
    }
}