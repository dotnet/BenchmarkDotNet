using System;
using System.Globalization;
using System.Reflection;
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
            if (value is string)
                return $"\"{value}\"";
            if (value is char)
                return $"'{value}'";
            if (value is float)
                return ((float) value).ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double)
                return ((double) value).ToString("G", CultureInfo.InvariantCulture) + "d";
            if (value is decimal)
                return ((decimal) value).ToString("G", CultureInfo.InvariantCulture) + "m";
            if (value.GetType().GetTypeInfo().IsEnum)
                return value.GetType().GetCorrectTypeName() + "." + value;
            if (value is Type)
                return "typeof(" + ((Type) value).GetCorrectTypeName() + ")";
            if (!value.GetType().GetTypeInfo().IsValueType)
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