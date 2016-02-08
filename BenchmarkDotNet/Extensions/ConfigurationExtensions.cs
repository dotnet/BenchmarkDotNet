using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Extensions
{
    internal static class ConfigurationExtensions
    {
        public static string ToConfig(this Platform platform)
        {
            switch (platform)
            {
                case Platform.AnyCpu:
                    return "AnyCPU";
                case Platform.X86:
                    return "x86";
                case Platform.X64:
                    return "x64";
                case Platform.Host:
                    return IntPtr.Size == 4 ? "x86" : "x64";
                default:
                    return "AnyCPU";
            }
        }

        public static string ToConfig(this Framework framework, Type benchmarkType)
        {
            if (framework == Framework.Host)
                return DetectCurrentFramework(benchmarkType);
            var number = framework.ToString().Substring(1);
            var numberArray = number.ToCharArray().Select(c => c.ToString()).ToArray();
            return "v" + string.Join(".", numberArray);
        }

        private static string DetectCurrentFramework(Type benchmarkType)
        {
            var attributes = benchmarkType.Assembly.GetCustomAttributes(false).OfType<Attribute>();
            var frameworkAttribute = attributes.FirstOrDefault(a => a.ToString() == @"System.Runtime.Versioning.TargetFrameworkAttribute");
            if (frameworkAttribute == null)
                return "v4.0";
            var frameworkName = frameworkAttribute.GetType()
                .GetProperty("FrameworkName", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(frameworkAttribute, null)?.ToString();
            if (frameworkName != null)
                frameworkName = frameworkName.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "";
            return frameworkName;
        }

        public static string ToConfig(this Jit jit)
        {
            return jit == Jit.LegacyJit ? "1" : "0";
        }
    }
}