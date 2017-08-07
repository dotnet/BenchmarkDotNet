using System;
using System.Globalization;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using SimpleJson.Reflection;

namespace BenchmarkDotNet.Core.Helpers
{
    public class SourceCodeHelper
    {
        public static string ToSourceCode(object value)
        {
            if (value == null)
                return "null";
            if (value is bool)
                return ((bool) value).ToLowerCase();
            if (value is string)
                return $"\"{value.ToString().Replace("\\", "\\\\")}\"";
            if (value is char)
                return (char) value == '\\' ? "'\\\\'" : $"'{value}'";
            if (value is float)
                return ((float) value).ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double)
                return ((double) value).ToString("G", CultureInfo.InvariantCulture) + "d";
            if (value is decimal)
                return ((decimal) value).ToString("G", CultureInfo.InvariantCulture) + "m";
            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return value.GetType().GetCorrectTypeName() + "." + value;
            if (value is Type)
                return "typeof(" + ((Type) value).GetCorrectTypeName() + ")";
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType)
                return "System.Activator.CreateInstance<" + value.GetType().GetCorrectTypeName() + ">()";
            if (value is TimeInterval)
                return "new BenchmarkDotNet.Horology.TimeInterval(" + ToSourceCode(((TimeInterval)value).Nanoseconds) + ")";
            if (value is IntPtr)
                return $"new System.IntPtr({value})";
            if (value is IFormattable)
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }
    }
}