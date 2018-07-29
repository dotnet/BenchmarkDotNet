using System;
using System.Globalization;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
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
                    return $"$@\"{text.Replace("\"", "\"\"").Replace("{", "{{").Replace("}", "}}")}\"";
                case char c:
                    return c == '\\' ? "'\\\\'" : $"'{value}'";
                case float f:
                    return f.ToString("G", CultureInfo.InvariantCulture) + "f";
                case double d:
                    return d.ToString("G", CultureInfo.InvariantCulture) + "d";
                case decimal f:
                    return f.ToString("G", CultureInfo.InvariantCulture) + "m";
            }

            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return value.GetType().GetCorrectCSharpTypeName() + "." + value;
            if (value is Type type)
                return "typeof(" + type.GetCorrectCSharpTypeName() + ")";
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType)
                return "System.Activator.CreateInstance<" + value.GetType().GetCorrectCSharpTypeName() + ">()";
            
            switch (value) {
                case TimeInterval interval:
                    return "new BenchmarkDotNet.Horology.TimeInterval(" + ToSourceCode(interval.Nanoseconds) + ")";
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