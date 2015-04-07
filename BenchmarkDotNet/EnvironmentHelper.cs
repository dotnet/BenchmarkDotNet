using System;
using System.Diagnostics;
using System.Reflection;

namespace BenchmarkDotNet
{
    internal static class EnvironmentHelper
    {
        private static string GetClrVersion()
        {
            var monoRuntimeType = Type.GetType("Mono.Runtime");
            var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
            if (monoDisplayName != null)
                return "Mono " + monoDisplayName.Invoke(null, null);
            return "MS.NET " + Environment.Version;
        }

        private static string GetArch()
        {
            return IntPtr.Size == 4 ? "32-bit" : "64-bit";
        }

        private static string GetConfiguration()
        {
            string configuration = "RELEASE";
#if DEBUG
            configuration = "DEBUG";
#endif
            return configuration;
        }

        private static string GetDebuggerFlag()
        {
            return Debugger.IsAttached ? " [AttachedDebugger]" : "";
        }

        private static string GetJitFlag()
        {
            if (Type.GetType("Mono.Runtime") == null && IntPtr.Size == 8 && GetConfiguration() == "RELEASE")
                if (!new JitHelper().IsMsX64())
                    return " [RyuJIT]";
            return "";
        }

        public static string GetFullEnvironmentInfo()
        {
            return $"CLR={GetClrVersion()}, Arch={GetArch()}, {GetConfiguration()}{GetDebuggerFlag()}{GetJitFlag()}";
        }

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
    }
}