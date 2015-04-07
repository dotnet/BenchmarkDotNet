using System.Collections.Generic;

namespace BenchmarkDotNet.Settings
{
    public static class BenchmarkSettings
    {
        public static readonly IBenchmarkSettingDefinition<bool> DetailedMode = new BenchmarkSettingDefinition<bool>(nameof(DetailedMode), false);
        public static readonly IBenchmarkSettingDefinition<uint> WarmUpIterationCount = new BenchmarkSettingDefinition<uint>(nameof(WarmUpIterationCount), 5);
        public static readonly IBenchmarkSettingDefinition<uint> MaxWarmUpIterationCount = new BenchmarkSettingDefinition<uint>(nameof(MaxWarmUpIterationCount), 30);
        public static readonly IBenchmarkSettingDefinition<double> MaxWarmUpError = new BenchmarkSettingDefinition<double>(nameof(MaxWarmUpError), 0.05);
        public static readonly IBenchmarkSettingDefinition<uint> TargetIterationCount = new BenchmarkSettingDefinition<uint>(nameof(TargetIterationCount), 10);
        public static readonly IBenchmarkSettingDefinition<uint> ProcessorAffinity = new BenchmarkSettingDefinition<uint>(nameof(ProcessorAffinity), 2);
        public static readonly IBenchmarkSettingDefinition<bool> AutoGcCollect = new BenchmarkSettingDefinition<bool>(nameof(AutoGcCollect), true);
        public static readonly IBenchmarkSettingDefinition<bool> HighPriority = new BenchmarkSettingDefinition<bool>(nameof(HighPriority), true);

        public static IDictionary<string, object> Build(params KeyValuePair<string, object>[] settings)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var setting in settings)
                dictionary[setting.Key] = setting.Value;
            return dictionary;
        }
    }
}