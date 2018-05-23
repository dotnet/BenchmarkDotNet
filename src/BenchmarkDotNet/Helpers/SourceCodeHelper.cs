using System;
using System.Globalization;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using SimpleJson.Reflection;

namespace BenchmarkDotNet.Helpers
{
    public class SourceCodeHelper
    {
        public static string ToSourceCode(object value)
        {
            if (value == null)
                return "null";
            if (value is bool)
                return ((bool) value).ToLowerCase();
            if (value is string text)
                return $"$@\"{value.ToString().Replace("\"", "\"\"").Replace("{", "{{").Replace("}", "}}")}\"";
            if (value is char)
                return (char) value == '\\' ? "'\\\\'" : $"'{value}'";
            if (value is float)
                return ((float) value).ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double)
                return ((double) value).ToString("G", CultureInfo.InvariantCulture) + "d";
            if (value is decimal)
                return ((decimal) value).ToString("G", CultureInfo.InvariantCulture) + "m";
            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return value.GetType().GetCorrectCSharpTypeName() + "." + value;
            if (value is Type)
                return "typeof(" + ((Type) value).GetCorrectCSharpTypeName() + ")";
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType)
                return "System.Activator.CreateInstance<" + value.GetType().GetCorrectCSharpTypeName() + ">()";
            if (value is TimeInterval)
                return "new BenchmarkDotNet.Horology.TimeInterval(" + ToSourceCode(((TimeInterval)value).Nanoseconds) + ")";
            if (value is IntPtr)
                return $"new System.IntPtr({value})";
            if (value is IFormattable)
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }

        public static bool IsCompilationTimeConstant(object value)
        {
            if (value == null)
                return true;
            if (value is bool)
                return true;
            if (value is string text)
                return true;
            if (value is char)
                return true;
            if (value is float)
                return true;
            if (value is double)
                return true;
            if (value is decimal)
                return true;
            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return true;
            if (value is Type)
                return true;
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType) // the difference!!
                return false;
            if (value is TimeInterval)
                return true;
            if (value is IntPtr)
                return true;
            if (value is IFormattable)
                return true;

            return false;
        }
    }
}