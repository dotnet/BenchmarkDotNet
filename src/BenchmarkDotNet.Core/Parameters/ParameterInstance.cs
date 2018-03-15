using BenchmarkDotNet.Code;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstance
    {
        public const string NullParameterTextRepresentation = "?";

        public ParameterDefinition Definition { get; }
        
        private readonly object value;

        public ParameterInstance(ParameterDefinition definition, object value)
        {
            Definition = definition;
            this.value = value;
        }

        public string Name => Definition.Name;
        public bool IsStatic => Definition.IsStatic;
        public bool IsArgument => Definition.IsArgument;

        public object Value => value is IParam parameter ? parameter.Value : value;

        public string ToSourceCode() 
            => value is IParam parameter 
                ? parameter.ToSourceCode()
                : SourceCodeHelper.ToSourceCode(value);

        public string ToDisplayText()
            => value is IParam parameter
                ? parameter.DisplayText
                : value?.ToString() 
                    ?? NullParameterTextRepresentation;

        public override string ToString() => ToDisplayText();
    }
}