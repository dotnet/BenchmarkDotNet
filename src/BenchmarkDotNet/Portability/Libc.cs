using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Portability
{
#pragma warning disable CS8981 // The type name 'libc' only contains lower-cased ascii characters. Such names may become reserved for the language.
    internal static class libc
#pragma warning restore CS8981
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
