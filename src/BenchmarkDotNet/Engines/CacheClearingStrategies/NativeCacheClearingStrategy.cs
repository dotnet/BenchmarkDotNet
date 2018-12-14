using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class NativeCacheClearingStrategy : ICacheClearingStrategy
    {
        [DllImport("kernel32.dll")]
        private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        public void ClearCache(IntPtr? affinity)
        {
            var process = Process.GetCurrentProcess();
            FlushInstructionCache(process.Handle, IntPtr.Zero, UIntPtr.Zero);
        }
    }
}