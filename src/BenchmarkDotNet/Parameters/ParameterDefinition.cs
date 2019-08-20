using System;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterDefinition
    {
        public string Name { get; }
        public bool IsStatic { get; }
        public object[] Values { get; }
        public bool IsArgument { get; }
        public Type ParameterType { get; }

        public ParameterDefinition(string name, bool isStatic, object[] values, bool isArgument, Type parameterType)
        {
            Name = name;
            IsStatic = isStatic;
            Values = values;
            IsArgument = isArgument;
            ParameterType = parameterType;
        }
    }
}