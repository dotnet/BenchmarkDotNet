using System;
using System.Globalization;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Code
{
    public class EnumParam : IParam
    {
        // Preserves type information for enum values from F# code
        // See also:
        // https://github.com/dotnet/fsharp/issues/995
        private readonly Type type;

        private EnumParam(object value, Type type)
        {
            this.Value = value;
            this.type = type;
        }

        public object Value { get; }

        public string DisplayText => $"{Enum.ToObject(type, Value)}";

        public string ToSourceCode() =>
            $"({type.GetCorrectCSharpTypeName()})({ToInvariantCultureString()})";

        internal static IParam FromObject(object value, Type? type = null)
        {
            type = type ?? value.GetType();
            if (!type.IsEnum)
                throw new ArgumentOutOfRangeException(nameof(type));

            return new EnumParam(value, type);
        }

        private string ToInvariantCultureString()
        {
            switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
            {
                case TypeCode.Byte:
                    return ((byte)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int16:
                    return ((short)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int32:
                    return ((int)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int64:
                    return ((long)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.SByte:
                    return ((sbyte)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt16:
                    return ((ushort)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt32:
                    return ((uint)Value).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt64:
                    return ((ulong)Value).ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Value));
            }
        }
    }
}
