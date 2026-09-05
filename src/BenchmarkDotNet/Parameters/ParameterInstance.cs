using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using System.Globalization;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstance : IDisposable, IAsyncDisposable
    {
        public const string NullParameterTextRepresentation = "?";

        [PublicAPI] public ParameterDefinition Definition { get; }

        /// <summary>
        /// The value bound to this benchmark case, and the description a toolchain uses to re-create it in
        /// generated code. The toolchain owns the generated syntax; this describes the value only.
        /// </summary>
        public ParameterValue ParameterValue { get; }

        private readonly int maxParameterColumnWidthFromConfig;

        public ParameterInstance(ParameterDefinition definition, ParameterValue parameterValue, SummaryStyle? summaryStyle)
        {
            Definition = definition;
            ParameterValue = parameterValue;
            maxParameterColumnWidthFromConfig = summaryStyle?.MaxParameterColumnWidth ?? SummaryStyle.DefaultMaxParameterColumnWidth;
        }

        /// <summary>Convenience overload for a value the toolchain can embed directly.</summary>
        internal ParameterInstance(ParameterDefinition definition, object? value, SummaryStyle? summaryStyle)
            : this(definition, new ParameterValue.Constant(value, definition.ParameterType), summaryStyle)
        {
        }

        public async ValueTask DisposeAsync()
        {
            switch (Value)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;

                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        public void Dispose()
        {
            switch (Value)
            {
                // Intentionally flipped the order from DisposeAsync to avoid sync-over-async if the value already supports sync dispose.
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IAsyncDisposable asyncDisposable:
                {
                    using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
                    context.ExecuteUntilComplete(asyncDisposable.DisposeAsync());
                    break;
                }
            }
        }

        public string Name => Definition.Name;
        public bool IsStatic => Definition.IsStatic;
        public bool IsArgument => Definition.IsArgument;

        public object? Value => ParameterValue.Value;

        private string ToDisplayText(CultureInfo cultureInfo, int maxParameterColumnWidth)
        {
            switch (Value)
            {
                case null:
                    return NullParameterTextRepresentation;
                case Array array:
                    return Trim(ArrayDisplay.GetDisplayString(array), maxParameterColumnWidth).EscapeSpecialCharacters(false);
                // An enum declared in F# is erased to its underlying type in attribute metadata, so the declared
                // type is what names the member (dotnet/fsharp#995). Matched on that underlying type exactly,
                // because a source can yield anything: any other value is a mismatch for someone else to report,
                // not something to crash Enum.ToObject on while rendering a display name.
                case var _ when Definition.ParameterType is { IsEnum: true }
                                && !Value!.GetType().IsEnum
                                && Value.GetType() == Enum.GetUnderlyingType(Definition.ParameterType):
                    return Trim(Enum.ToObject(Definition.ParameterType, Value).ToString()!, maxParameterColumnWidth).EscapeSpecialCharacters(false);
                case IFormattable formattable:
                    return Trim(formattable.ToString(null, cultureInfo), maxParameterColumnWidth).EscapeSpecialCharacters(false);
                // no trimming for types!
                case Type type:
                    return type.IsNullable() ? $"{Nullable.GetUnderlyingType(type)!.GetDisplayName()}?" : type.GetDisplayName();
                default:
                    return Trim(Value.ToString()!, maxParameterColumnWidth).EscapeSpecialCharacters(false);
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

            if (IsFirstCharInSurrogatePair(value[takeFromStart - 1]))
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
