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
                    return Trim(parameter.DisplayText, maxParameterColumnWidth).EscapeSpecialCharacters(false);
                case IFormattable formattable:
                    return Trim(formattable.ToString(null, cultureInfo), maxParameterColumnWidth).EscapeSpecialCharacters(false);
                // no trimming for types!
                case Type type:
                    return type.IsNullable() ? $"{Nullable.GetUnderlyingType(type).GetDisplayName()}?" : type.GetDisplayName();
                default:
                    return Trim(value.ToString(), maxParameterColumnWidth).EscapeSpecialCharacters(false);
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

            var takeFromStart = (maxDisplayTextInnerLength - postfix.Length - dots.Length) / 2;
            var takeFromEnd = takeFromStart;

            if (IsFirstCharInSurrogatePair(value[takeFromStart-1]))
            {
                takeFromStart = Math.Max(0, takeFromStart - 1);
            }

            if (IsSecondCharInSurrogatePair(value[value.Length - takeFromEnd]))
            {
                takeFromEnd = Math.Max(0, takeFromEnd - 1);
            }

            var result = value.Substring(0, takeFromStart) + dots + value.Substring(value.Length - takeFromEnd, takeFromEnd) + postfix;

            return result;
        }

        private static bool IsFirstCharInSurrogatePair(char c)
        {
            return BitConverter.IsLittleEndian ? char.IsHighSurrogate(c) : char.IsLowSurrogate(c);
        }

        private static bool IsSecondCharInSurrogatePair(char c)
        {
            return BitConverter.IsLittleEndian ? char.IsLowSurrogate(c) : char.IsHighSurrogate(c);
        }
    }
}
