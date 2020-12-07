using System;
using System.Globalization;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstance : IDisposable
    {
        public const string NullParameterTextRepresentation = "?";

        [PublicAPI] public ParameterDefinition Definition { get; }

        private readonly object value;
        private readonly int maxParameterColumnWidthFromConfig;

        public ParameterInstance(ParameterDefinition definition, object value, SummaryStyle summaryStyle)
        {
            Definition = definition;
            this.value = value;
            maxParameterColumnWidthFromConfig = summaryStyle?.MaxParameterColumnWidth ?? SummaryStyle.DefaultMaxParameterColumnWidth;
        }

        public void Dispose() => (Value as IDisposable)?.Dispose();

        public string Name => Definition.Name;
        public bool IsStatic => Definition.IsStatic;
        public bool IsArgument => Definition.IsArgument;

        public object Value => value is IParam parameter ? parameter.Value : value;

        public string ToSourceCode()
            => value is IParam parameter
                ? parameter.ToSourceCode()
                : SourceCodeHelper.ToSourceCode(value);

        private string ToDisplayText(CultureInfo cultureInfo, int maxParameterColumnWidth)
        {
            switch (value)
            {
                case null:
                    return NullParameterTextRepresentation;
                case IParam parameter:
                    return Trim(parameter.DisplayText, maxParameterColumnWidth);
                case IFormattable formattable:
                    return Trim(formattable.ToString(null, cultureInfo), maxParameterColumnWidth);
                // no trimming for types!
                case Type type:
                    return type.IsNullable() ? $"{Nullable.GetUnderlyingType(type).GetDisplayName()}?" : type.GetDisplayName();
                default:
                    return Trim(value.ToString(), maxParameterColumnWidth);
            }
        }

        public string ToDisplayText(SummaryStyle summary)
        {
            return summary != null ? ToDisplayText(summary.CultureInfo, summary.MaxParameterColumnWidth) : ToDisplayText();
        }

        public string ToDisplayText() => ToDisplayText(CultureInfo.CurrentCulture, maxParameterColumnWidthFromConfig);

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
