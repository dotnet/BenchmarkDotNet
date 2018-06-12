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
                return $"$@\"{text.Replace("\"", "\"\"").Replace("{", "{{").Replace("}", "}}")}\"";
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
            => value == null || IsCompilationTimeConstant(value.GetType());

        public static bool IsCompilationTimeConstant(Type type)
        {
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
            if (!type.IsValueType) // the difference!!
                return false;
            if (type == typeof(TimeInterval))
                return true;
            if (type == typeof(IntPtr))
                return true;
            if (typeof(IFormattable).IsAssignableFrom(type))
                return true;

            return false;
        }
    }
}