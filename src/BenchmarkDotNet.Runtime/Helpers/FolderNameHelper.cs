﻿using System;
using System.Globalization;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;

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
            if (value.GetType().GetTypeInfo().IsEnum)
                return value.ToString();
            if (value is Type)
                return ((Type)value).Name; // TODO
            if (!value.GetType().GetTypeInfo().IsValueType)
                return value.GetType().Name; // TODO
            if (value is TimeInterval)
                return ((TimeInterval) value).Nanoseconds + "ns";
            return value.ToString();
        }

        private static string Escape(string value)
        {
            return value; // TODO: escape special symbols
        }
    }
}