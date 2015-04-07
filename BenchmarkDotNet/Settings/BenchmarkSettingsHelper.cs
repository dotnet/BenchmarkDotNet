using System.Collections.Generic;

namespace BenchmarkDotNet.Settings
{
    public static class BenchmarkSettingsHelper
    {
        public static Dictionary<string, object> Union(params IDictionary<string, object>[] benchmarkSettingsArray)
        {
            var settings = new Dictionary<string, object>();
            foreach (var benchmarkSettings in benchmarkSettingsArray)
                if (benchmarkSettings != null)
                    foreach (var setting in benchmarkSettings)
                        if (!settings.ContainsKey(setting.Key))
                            settings[setting.Key] = setting.Value;
            return settings;
        }
    }
}