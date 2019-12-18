using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using SimpleJson.Reflection;

namespace BenchmarkDotNet.Helpers
{
    internal static class ArtifactFileNameHelper
    {
        public static string GetFilePath(DiagnoserActionParameters details, DateTime creationTime, string fileExtension)
        {
            string fileName = $@"{FolderNameHelper.ToFolderName(details.BenchmarkCase.Descriptor.Type)}.{FullNameProvider.GetMethodName(details.BenchmarkCase)}";

            // if we run for more than one toolchain, the output file name should contain the name too so we can differ net461 vs netcoreapp2.1 etc
            if (details.Config.GetJobs().Select(job => job.GetToolchain()).Distinct().Count() > 1)
                fileName += $"-{details.BenchmarkCase.Job.Environment.Runtime?.Name ?? details.BenchmarkCase.GetToolchain()?.Name ?? details.BenchmarkCase.Job.Id}";

            fileName += $"-{creationTime.ToString(BenchmarkRunnerClean.DateTimeFormat)}";

            fileName = FolderNameHelper.ToFolderName(fileName);

            return Path.Combine(details.Config.ArtifactsPath, $"{fileName}.{fileExtension}");
        }
    }
    public static class FolderNameHelper
    {
        public static string ToFolderName(object value)
        {
            switch (value) {
                case bool b:
                    return b.ToLowerCase();
                case string s:
                    return Escape(new StringBuilder(s));
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

        // we can't simply use type.FullName, because for generics it's too long
        // example: typeof(List<int>).FullName => "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
        public static string ToFolderName(Type type, bool includeNamespace = true, bool includeGenericArgumentsNamespace = false)
            => Escape(new StringBuilder(type.GetCorrectCSharpTypeName(includeNamespace, includeGenericArgumentsNamespace)));

        private static string Escape(StringBuilder builder)
        {
            foreach (char invalidPathChar in Path.GetInvalidFileNameChars())
                builder.Replace(invalidPathChar, '_');

            // > and < are valid on Unix, but not on Windows. File names should be consistent across all OSes #981
            builder.Replace('<', '_');
            builder.Replace('>', '_');

            return builder.ToString();
        }
    }
}