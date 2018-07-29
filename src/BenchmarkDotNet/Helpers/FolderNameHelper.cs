using System;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using SimpleJson.Reflection;

namespace BenchmarkDotNet.Helpers
{
    public static class FolderNameHelper
    {
        public static string ToFolderName(object value)
        {
            switch (value) {
                case bool b:
                    return b.ToLowerCase();
                case string s:
                    return Escape(s);
                case char c:
                    return ((int)c).ToString(); // TODO: rewrite
                case float f:
                    return f.ToString("F", CultureInfo.InvariantCulture).Replace(".", "-");
                case double d:
                    return d.ToString("F", CultureInfo.InvariantCulture).Replace(".", "-");
                case decimal d:
                    return d.ToString("F", CultureInfo.InvariantCulture).Replace(".", "-");
            }

            if (ReflectionUtils.GetTypeInfo(value.GetType()).IsEnum)
                return value.ToString();
            if (value is Type type)
                return ToFolderName(type: type);
            if (!ReflectionUtils.GetTypeInfo(value.GetType()).IsValueType)
                return value.GetType().Name; // TODO
            if (value is TimeInterval interval)
                return interval.Nanoseconds + "ns";
            return value.ToString();
        }

        private static string Escape(string value)
        {
            return value; // TODO: escape special symbols
        }

        // we can't simply use type.FullName, because for generics it's too long
        // example: typeof(List<int>).FullName => "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
        public static string ToFolderName(Type type)
            => new StringBuilder(type.GetCorrectCSharpTypeName(includeGenericArgumentsNamespace: false))
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('[', '_')
                .Replace(']', '_')
                .ToString();
    }
}