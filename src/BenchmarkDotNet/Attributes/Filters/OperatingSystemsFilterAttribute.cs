using System;
using System.Linq;
using BenchmarkDotNet.Filters;
using JetBrains.Annotations;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class OperatingSystemsFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        public OperatingSystemsFilterAttribute() { }

        /// <param name="allowed">if set to true, the OSes beloning to platforms are enabled, if set to false, disabled</param>
        public OperatingSystemsFilterAttribute(bool allowed, params PlatformID[] platforms)
            : base(new SimpleFilter(_ =>
            {
                return allowed
                    ? platforms.Any(platform => RuntimeInformation.IsOSPlatform(Map(platform)))
                    : platforms.All(platform => !RuntimeInformation.IsOSPlatform(Map(platform)));
            }))
        {
        }

        // OSPlatform is a struct so it can not be used as attribute argument and this is why we use PlatformID enum
        private static OSPlatform Map(PlatformID platform)
        {
            switch (platform)
            {
                case PlatformID.MacOSX:
                    return OSPlatform.OSX;
                case PlatformID.Unix:
                    return OSPlatform.Linux;
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return OSPlatform.Windows;
                default:
                    throw new NotSupportedException($"Platform {platform} is not supported");
            }
        }
    }
}