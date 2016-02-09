namespace BenchmarkDotNet.Parameters
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public bool IsStatic { get; }
        public object[] Values { get; }

        public ParameterDefinition(string name, bool isStatic, object[] values)
        {
            Name = name;
            IsStatic = isStatic;
            Values = values;
        }
    }
}