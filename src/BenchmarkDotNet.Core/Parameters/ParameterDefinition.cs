namespace BenchmarkDotNet.Parameters
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public bool IsStatic { get; }
        public object[] Values { get; }
        public bool IsArgument { get; }

        public ParameterDefinition(string name, bool isStatic, object[] values, bool isArgument)
        {
            Name = name;
            IsStatic = isStatic;
            Values = values;
            IsArgument = isArgument;
        }
    }
}