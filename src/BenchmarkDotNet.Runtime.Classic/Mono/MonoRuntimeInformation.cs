using System;
using System.Reflection;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Mono
{
    internal class MonoRuntimeInformation : Portability.RuntimeInformation
    {
        public override bool IsMono => true;

        public override bool IsWindows => Environment.OSVersion.Platform.ToString().Contains("Win");

        public override bool IsLinux => System.Environment.OSVersion.Platform == PlatformID.Unix
            && GetSysnameFromUname().Equals("Linux", StringComparison.InvariantCultureIgnoreCase);

        public override bool IsMac => System.Environment.OSVersion.Platform == PlatformID.Unix
            && GetSysnameFromUname().Equals("Darwin", StringComparison.InvariantCultureIgnoreCase);

        public override string GetProcessorName() => Unknown;

        public override Runtime CurrentRuntime => Runtime.Mono;

        public override string GetRuntimeVersion()
        {
            var monoRuntimeType = Type.GetType("Mono.Runtime");
            var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
            if (monoDisplayName == null)
                return Unknown;

            string version = monoDisplayName.Invoke(null, null)?.ToString();
            if (version != null)
            {
                int bracket1 = version.IndexOf('('), bracket2 = version.IndexOf(')');
                if (bracket1 != -1 && bracket2 != -1)
                {
                    string comment = version.Substring(bracket1 + 1, bracket2 - bracket1 - 1);
                    var commentParts = comment.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (commentParts.Length > 2)
                        version = version.Substring(0, bracket1) + "(" + commentParts[0] + " " + commentParts[1] + ")";
                }
            }
            return "Mono " + version;
        }

        public override bool HasRyuJit => false;

        public override string JitInfo => Unknown;

        public override string GetConfiguration()
        {
            bool? isDebug = Assembly.GetEntryAssembly().IsDebug();
            if (isDebug.HasValue == false)
            {
                return Unknown;
            }
            return isDebug.Value ? DebugConfigurationName : ReleaseConfigurationName;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int uname(IntPtr buf);

        private static string GetSysnameFromUname()
        {
            var buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                int rc = uname(buf);
                if (rc != 0)
                {
                    throw new Exception("uname from libc returned " + rc);
                }

                string os = Marshal.PtrToStringAnsi(buf);
                return os;
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
        }
    }
}