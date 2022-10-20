using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using BenchmarkDotNet.Extensions;
using Perfolizer.Horology;
using SimpleJson.Reflection;

namespace BenchmarkDotNet.Helpers
{
    public static class SourceCodeHelper
    {
        public static string ToSourceCode(object value)
        {
            switch (value) {
                case null:
                    return "null";
                case bool b:
                    return b.ToLowerCase();
                case string text:
                    return text.EscapeSpecialCharacters(true);
                case char c:
                    return c.EscapeSpecialCharacter(true);
                case float f:
                    return ToSourceCode(f);
                case double d:
                    return ToSourceCode(d);
                case decimal f:
                    return f.ToString("G", CultureInfo.InvariantCulture) + "m";
                case BigInteger bigInteger:
                    return $"System.Numerics.BigInteger.Parse(\"{bigInteger.ToString(CultureInfo.InvariantCulture)}\", System.Globalization.CultureInfo.InvariantCulture)";
                case DateTime dateTime:
                    return $"System.DateTime.Parse(\"{dateTime.ToString(CultureInfo.InvariantCulture)}\", System.Globalization.CultureInfo.InvariantCulture)";
                case Guid guid:
                    return $"System.Guid.Parse(\"{guid.ToString()}\")";
            }
            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return $"({value.GetType().GetCorrectCSharpTypeName()})({ToInvariantCultureString(value)})";
            if (value is Type type)
                return "typeof(" + type.GetCorrectCSharpTypeName() + ")";
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType)
                return "System.Activator.CreateInstance<" + value.GetType().GetCorrectCSharpTypeName() + ">()";

            switch (value) {
                case TimeInterval interval:
                    return "new Perfolizer.Horology.TimeInterval(" + ToSourceCode(interval.Nanoseconds) + ")";
                case IntPtr ptr:
                    return $"new System.IntPtr({ptr})";
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        public static bool IsCompilationTimeConstant(object value)
            => value == null || IsCompilationTimeConstant(value.GetType());

        public static bool IsCompilationTimeConstant(Type type)
        {
            if (type == typeof(long) || type == typeof(ulong))
                return true;
            if (type == typeof(int) || type == typeof(uint))
                return true;
            if (type == typeof(short) || type == typeof(ushort))
                return true;
            if (type == typeof(byte) || type == typeof(sbyte))
                return true;
            if (type == typeof(bool))
                return true;
            if (type == typeof(string))
                return true;
            if (type == typeof(char))
                return true;
            if (type == typeof(float))
                return true;
            if (type == typeof(double))
                return true;
            if (type == typeof(decimal))
                return true;
            if (type.IsEnum)
                return true;
            if (type == typeof(Type))
                return true;
            if (type == typeof(TimeInterval))
                return true;
            if (type == typeof(IntPtr))
                return true;
            if (type == typeof(DateTime))
                return true;
            if (!type.IsValueType) // the difference!!
                return false;
            if (typeof(IFormattable).IsAssignableFrom(type))
                return false;

            return false;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static string ToSourceCode(double value)
        {
            if (double.IsNaN(value))
                return "System.Double.NaN";
            if (double.IsPositiveInfinity(value))
                return "System.Double.PositiveInfinity";
            if (double.IsNegativeInfinity(value))
                return "System.Double.NegativeInfinity";
            if (value == double.Epsilon)
                return "System.Double.Epsilon";
            if (value == double.MaxValue)
                return "System.Double.MaxValue";
            if (value == double.MinValue)
                return "System.Double.MinValue";

            return value.ToString("G", CultureInfo.InvariantCulture) + "d";
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static string ToSourceCode(float value)
        {
            if (float.IsNaN(value))
                return "System.Single.NaN";
            if (float.IsPositiveInfinity(value))
                return "System.Single.PositiveInfinity";
            if (float.IsNegativeInfinity(value))
                return "System.Single.NegativeInfinity";
            if (value == float.Epsilon)
                return "System.Single.Epsilon";
            if (value == float.MaxValue)
                return "System.Single.MaxValue";
            if (value == float.MinValue)
                return "System.Single.MinValue";

            return value.ToString("G", CultureInfo.InvariantCulture) + "f";
        }

        private static string ToInvariantCultureString(object @enum)
        {
            switch (Type.GetTypeCode(Enum.GetUnderlyingType(@enum.GetType())))
            {
                case TypeCode.Byte:
                    return ((byte)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int16:
                    return ((short)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int32:
                    return ((int)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.Int64:
                    return ((long)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.SByte:
                    return ((sbyte)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt16:
                    return ((ushort)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt32:
                    return ((uint)@enum).ToString(CultureInfo.InvariantCulture);
                case TypeCode.UInt64:
                    return ((ulong)@enum).ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(@enum));
            }
        }
    }
}
