using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using RuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

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

        [HandleProcessCorruptedStateExceptions] // #276
        [SecurityCritical]
        private static bool Initialize(out long qpf)
        {
            if (!RuntimeInformation.IsWindows())
            {
                qpf = default;
                return false;
            }

            try
            {
                return QueryPerformanceFrequency(out qpf) && QueryPerformanceCounter(out _);
            }
            catch
            {
                qpf = default;
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