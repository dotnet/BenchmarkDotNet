#if !CORE
using System.Runtime.ExceptionServices;
using System.Security;
#endif
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Horology
{
    internal class WindowsClock : IClock
    {
        private static readonly bool GlobalIsAvailable;
        private static readonly long GlobalFrequency;

        static WindowsClock() => GlobalIsAvailable = Initialize(out GlobalFrequency);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long value);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long value);

#if !CORE
        [HandleProcessCorruptedStateExceptions] // #276
        [SecurityCritical]
#endif
        private static bool Initialize(out long qpf)
        {
            if (!Portability.RuntimeInformation.IsWindows())
            {
                qpf = default(long);
                return false;
            }

            try
            {
                return QueryPerformanceFrequency(out qpf) && QueryPerformanceCounter(out _);
            }
            catch
            {
                qpf = default(long);
                return false;
            }
        }

        public string Title => "Windows";
        public bool IsAvailable => GlobalIsAvailable;
        public Frequency Frequency => new Frequency(GlobalFrequency);

        public long GetTimestamp()
        {
            QueryPerformanceCounter(out long value);
            return value;
        }
    }
}