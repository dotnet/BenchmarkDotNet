using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Full
{
    internal class ClassicRuntimeInformation : Portability.RuntimeInformation
    {
        public override bool IsWindows => true;

        public override bool IsLinux => false;

        public override bool IsMac => false;

        public override string GetProcessorName()
        {
            try
            {
                string info = string.Empty;
                var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
                    info += moProcessor["name"]?.ToString();

                return ProcessorBrandStringHelper.Prettify(info);
            }
            catch
            {
                return Unknown;
            }
        }

        public override Runtime CurrentRuntime => Runtime.Clr;

        public override string GetRuntimeVersion() => $"Clr {Environment.Version}";

        public override bool HasRyuJit 
            => IntPtr.Size == 8
                && GetConfiguration() != DebugConfigurationName
                && !new JitHelper().IsMsX64();

        public override string JitInfo
        {
            get
            {
                // We are working on Full CLR, so there are only LegacyJIT and RyuJIT
                var modules = JitModules.ToArray();
                string jitName = HasRyuJit ? "RyuJIT" : "LegacyJIT";
                if (modules.Length == 1)
                {
                    // If we have only one JIT module, we know the version of the current JIT compiler
                    return jitName + "-v" + modules[0].Version;
                }
                else
                {
                    // Otherwise, let's just print information about all modules
                    return jitName + "/" + JitModulesInfo;
                }
            }
        }

        protected override IEnumerable<JitModule> JitModules
            => Process
                .GetCurrentProcess()
                .Modules.OfType<ProcessModule>()
                .Where(module => module.ModuleName.Contains("jit"))
                .Select(module => new JitModule(Path.GetFileNameWithoutExtension(module.FileName), module.FileVersionInfo.ProductVersion));

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
        private class JitHelper
        {
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                var value = 0;
                for (int i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (int j = 0; j < 2 * step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }

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