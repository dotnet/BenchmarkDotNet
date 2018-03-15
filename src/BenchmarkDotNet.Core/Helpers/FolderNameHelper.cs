using System;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using SimpleJson.Reflection;

namespace BenchmarkDotNet.Helpers
{
    public class FolderNameHelper
    {
        public static string ToFolderName(object value)
        {
            if (value is bool)
                return ((bool)value).ToLowerCase();
            if (value is string)
                return Escape((string)value);
            if (value is char)
                return ((int)(char)value).ToString(); // TODO: rewrite
            if (value is float)
                return ((float)value).ToString("F", CultureInfo.InvariantCulture).Replace(".", "-");
            if (value is double)
                return ((double)value).ToString("F", CultureInfo.InvariantCulture).Replace(".", "-");
            if (value is decimal)
                return ((decimal)value).ToString("F", CultureInfo.InvariantCulture).Replace(".", "-");
            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return value.ToString();
            if (value is Type type)
                return ToFolderName(type: type);
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType)
                return value.GetType().Name; // TODO
            if (value is TimeInterval)
                return ((TimeInterval) value).Nanoseconds + "ns";
            return value.ToString();
        }

        private static string Escape(string value)
        {
            return value; // TODO: escape special symbols
        }

        // we can't simply use type.FullName, because for generics it's tooo long
        // example: typeof(List<int>).FullName => "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
        public static string ToFolderName(Type type)
            => new StringBuilder(type.GetDisplayName())
                .Replace('<', '_')
                .Replace('>', '_')
                .ToString();
    }
}