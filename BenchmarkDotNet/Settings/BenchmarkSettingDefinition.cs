namespace BenchmarkDotNet.Settings
{
    public class BenchmarkSettingDefinition<T> : IBenchmarkSettingDefinition<T>
    {
        public string Name { get; }
        public T DefaultValue { get; }

        public BenchmarkSettingDefinition(string name, T defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}