using System.Collections.Generic;

namespace BenchmarkDotNet.Settings
{
    public static class BenchmarkSettingsExtensions
    {
        public static KeyValuePair<string, object> Create<T>(this IBenchmarkSettingDefinition<T> settingDefinition, T value)
        {
            return new KeyValuePair<string, object>(settingDefinition.Name, value);
        }

        public static void Set<T>(this IBenchmarkSettingDefinition<T> settingDefinition, IDictionary<string, object> settings, T value)
        {
            settings[settingDefinition.Name] = value;
        }

        public static T Get<T>(this IBenchmarkSettingDefinition<T> settingDefinition, IDictionary<string, object> benchmarkSettings)
        {
            var name = settingDefinition.Name;
            if (benchmarkSettings != null && benchmarkSettings.ContainsKey(name) && benchmarkSettings[name] is T)
                return (T)benchmarkSettings[name];
            return settingDefinition.DefaultValue;
        }
    }
}