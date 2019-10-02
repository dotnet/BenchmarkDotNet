using System;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstance
    {
        public const string NullParameterTextRepresentation = "?";
        internal const int DefaultMaxDisplayTextInnerLength = 15 + 5; // 5 is for postfix " [15]"

        [PublicAPI] public ParameterDefinition Definition { get; }

        private readonly object value;
        private readonly ImmutableConfig config;

        public ParameterInstance(ParameterDefinition definition, object value, ImmutableConfig config)
        {
            Definition = definition;
            this.value = value;
            this.config = config;
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
        {
            switch (value) {
                case null:
                    return NullParameterTextRepresentation;
                case IParam parameter:
                    return Trim(parameter.DisplayText, config.SummaryStyle?.MaxParamterColumnWidth ?? DefaultMaxDisplayTextInnerLength);
                // no trimming for types!
                case Type type:
                    return type.IsNullable() ? $"{Nullable.GetUnderlyingType(type).GetDisplayName()}?" : type.GetDisplayName();
            }

            return Trim(value.ToString(), config.SummaryStyle?.MaxParamterColumnWidth ?? DefaultMaxDisplayTextInnerLength);
        }

        public override string ToString() => ToDisplayText();

        private static string Trim(string value, int maxDisplayTextInnerLength)
        {
            if (value.Length <= maxDisplayTextInnerLength)
                return value;

            var postfix = $" [{value.Length}]";
            const string dots = "(...)";
            int take = (maxDisplayTextInnerLength - postfix.Length - dots.Length) / 2;

            return value.Substring(0, take) + dots + value.Substring(value.Length - take, take) + postfix;
        }
    }
}