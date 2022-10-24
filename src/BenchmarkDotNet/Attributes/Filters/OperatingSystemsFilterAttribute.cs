using System;
using System.Linq;
using BenchmarkDotNet.Filters;
using JetBrains.Annotations;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Attributes
{
    public enum OS : byte
    {
        Windows,
        Linux,
        macOS,
        /// <summary>
        /// WebAssembly
        /// </summary>
        Browser
    }

    [PublicAPI]
    public class OperatingSystemsFilterAttribute : FilterConfigBaseAttribute
    {
        private static readonly OSPlatform browser = OSPlatform.Create("BROWSER");

        // CLS-Compliant Code requires a constructor without an array in the argument list
        public OperatingSystemsFilterAttribute() { }

        /// <param name="allowed">if set to true, the OSes belonging to platforms are enabled, if set to false, disabled</param>
        /// <param name="platforms">the platform(s) for which the filter should be applied</param>
        public OperatingSystemsFilterAttribute(bool allowed, params OS[] platforms)
            : base(new SimpleFilter(_ =>
            {
                return allowed
                    ? platforms.Any(platform => RuntimeInformation.IsOSPlatform(Map(platform)))
                    : platforms.All(platform => !RuntimeInformation.IsOSPlatform(Map(platform)));
            }))
        {
        }

        // OSPlatform is a struct so it can not be used as attribute argument and this is why we use PlatformID enum
        private static OSPlatform Map(OS platform)
        {
            switch (platform)
            {
                case OS.Windows:
                    return OSPlatform.Windows;
                case OS.Linux:
                    return OSPlatform.Linux;
                case OS.macOS:
                    return OSPlatform.OSX;
                case OS.Browser:
                    return browser;
                default:
                    throw new NotSupportedException($"Platform {platform} is not supported");
            }
        }
    }
}