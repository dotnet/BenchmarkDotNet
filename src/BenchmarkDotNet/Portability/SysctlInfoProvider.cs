using System;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability
{
    /// <summary>    
    /// CPU information from output of the `sysctl -a` command.
    /// MacOSX only.
    /// </summary>
    internal static class SysctlInfoProvider
    {
        internal static readonly Lazy<string> SysctlInfo = new Lazy<string>(Load);
        private static string _sysctlInfo = null;

        [CanBeNull]
        private static string Load()
        {
            // Check if value is already computed
            if (_sysctlInfo != null)
            {
                return _sysctlInfo;
            }

            if (RuntimeInformation.IsMacOSX())
            {
                string content = ProcessHelper.RunAndReadOutput("sysctl", "-a");
                _sysctlInfo = content;
                return content;
            }
            return null;
        }
    }
}