namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstance
    {
        public ParameterDefinition Definition { get; }
        public object Value { get; }
        public string Name => Definition.Name;
        public bool IsStatic => Definition.IsStatic;

        public ParameterInstance(ParameterDefinition definition, object value)
        {
            Definition = definition;
            Value = value;
        }
    }
}