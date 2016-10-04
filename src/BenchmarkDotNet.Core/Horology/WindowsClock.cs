using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;

namespace BenchmarkDotNet.Horology
{
    public class WindowsClock : IClock
    {
        private static readonly bool isAvailable;
        private static readonly long frequency;
        
        static WindowsClock()
        {
            isAvailable = Initialize(out frequency);
        }

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long value);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long value);

        public bool IsAvailable => isAvailable;
        public Frequency Frequency => new Frequency(frequency);
        
        public long GetTimestamp()
        {
            long value;
            QueryPerformanceCounter(out value);
            return value;
        }

#if !CORE
        [HandleProcessCorruptedStateExceptions] // #276
        [SecurityCritical]
#endif
        static bool Initialize(out long frequency)
        {
            try
            {
                long counter;
                return 
                    QueryPerformanceFrequency(out frequency) &&
                    QueryPerformanceCounter(out counter);
            }
            catch
            {
                frequency = default(long);

                return false;
            }
        }
    }
}