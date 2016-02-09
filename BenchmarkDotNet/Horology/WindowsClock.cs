using System;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Horology
{
    public class WindowsClock : IClock
    {
        private static readonly bool isAvailable;
        private static readonly long frequency;

        static WindowsClock()
        {
            try
            {
                long counter;
                isAvailable = 
                    QueryPerformanceFrequency(out frequency) && 
                    QueryPerformanceCounter(out counter);
            }
            catch (Exception e)
            {
                isAvailable = false;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long value);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long value);

        public bool IsAvailable => isAvailable;
        public long Frequency => frequency;
        
        public long GetTimestamp()
        {
            long value;
            QueryPerformanceCounter(out value);
            return value;
        }
    }
}