namespace BenchmarkDotNet.Jobs
{
    public class EnvironmentVariable
    {
        public EnvironmentVariable(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }

        // CharacteristicPresenters call ToString(), this is why we need this override
        public override string ToString() => $"{Key}={Value}";
    }
}