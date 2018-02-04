using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Disassembler
{
    internal class Native
    {
        //https://stackoverflow.com/questions/35096660/check-avx-instruction-set-support
        public static bool HasAvxSupport
        {
            get
            {
                try
                {
                    return (GetEnabledXStateFeatures() & 4) != 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        //https://msdn.microsoft.com/ru-ru/library/windows/desktop/hh134235(v=vs.85).aspx
        [DllImport("kernel32.dll")]
        private static extern long GetEnabledXStateFeatures();
    }
}
