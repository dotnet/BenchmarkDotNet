using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Portability
{
    internal static class libc
    {
        [DllImport(nameof(libc))]
        internal static extern int getppid();

        [DllImport(nameof(libc))]
        internal static extern uint getuid();

        [DllImport(nameof(libc), SetLastError = true)]
        internal static extern int kill(int pid, int sig);

        [DllImport(nameof(libc), SetLastError = true)]
        internal static extern int chmod(string path, uint mode);

        internal static class Signals
        {
            internal const int SIGINT = 2;
        }

        internal static class FilePermissions
        {
            internal const uint S_IXUSR = 0x40u;
        }
    }
}
