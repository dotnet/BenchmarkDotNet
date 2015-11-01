using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkTask
    {
        public int ProcessCount { get; }
        public BenchmarkConfiguration Configuration { get; }
        public BenchmarkParametersSets ParametersSets { get; }

        public string Caption => Configuration.Caption;
        public string Description => Configuration.Caption + (!ParametersSets.IsEmpty() ? $" {ParametersSets.Description}" : string.Empty) ;

        public BenchmarkTask(int processCount, BenchmarkConfiguration configuration, BenchmarkParametersSets parametersSets = null)
        {
            ProcessCount = processCount;
            Configuration = configuration;
            ParametersSets = parametersSets ?? BenchmarkParametersSets.Empty;
        }

        public static IEnumerable<BenchmarkTask> Resolve(MethodInfo methodInfo)
        {
            var attrs = methodInfo.GetCustomAttributes(typeof(BenchmarkTaskAttribute), false).Cast<BenchmarkTaskAttribute>().ToList();
            if (attrs.Count == 0 && methodInfo.DeclaringType != null)
                attrs = methodInfo.DeclaringType.GetCustomAttributes(typeof(BenchmarkTaskAttribute), false).Cast<BenchmarkTaskAttribute>().ToList();
            if (attrs.Count == 0)
                attrs.Add(new BenchmarkTaskAttribute());
            return attrs.Select(attr => attr.Task);
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(ProcessCount), ProcessCount.ToString());
                foreach (var property in Configuration.Properties)
                    yield return property;
                foreach (var property in ParametersSets.Properties)
                    yield return property;
            }
        }
    }
}