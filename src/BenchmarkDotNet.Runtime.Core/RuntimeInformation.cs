using System;
using System.Reflection;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet
{
    public class RuntimeInformation : Portability.RuntimeInformation
    {
        public override bool IsWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public override bool IsLinux => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public override bool IsMac => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public override string GetProcessorName()
        {
            if (IsWindows)
                return ProcessorBrandStringHelper.Prettify(ExternalToolsHelper.Wmic.Value.GetValueOrDefault("Name") ?? "");

            if (IsLinux)
                return ProcessorBrandStringHelper.Prettify(ExternalToolsHelper.ProcCpuInfo.Value.GetValueOrDefault("model name") ?? "");

            if (IsMac)
                return ProcessorBrandStringHelper.Prettify(ExternalToolsHelper.Sysctl.Value.GetValueOrDefault("machdep.cpu.brand_string") ?? "");

            throw new PlatformNotSupportedException();
        }

        public override Runtime CurrentRuntime => Runtime.Core;

        public override string GetRuntimeVersion() => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        public override bool HasRyuJit => true; // https://github.com/dotnet/announcements/issues/10

        public override string JitInfo => "RyuJIT";

        public override string GetConfiguration()
        {
            bool? isDebug = Assembly.GetEntryAssembly().IsDebug();
            if (isDebug.HasValue == false)
            {
                return Unknown;
            }
            return isDebug.Value ? DebugConfigurationName : ReleaseConfigurationName;
        }
    }
}