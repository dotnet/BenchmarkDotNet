namespace BenchmarkDotNet.Settings
{
    public interface IBenchmarkSettingDefinition<out T>
    {
        string Name { get; }
        T DefaultValue { get; }
    }
}