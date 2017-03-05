#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    public enum ProcessPriorityClass
    {
        Normal = 32,
        Idle = 64,
        High = 128,
        RealTime = 256,
        BelowNormal = 16384,
        AboveNormal = 32768
    }

    public class Process
    {
        public static Process GetCurrentProcess()
        {
            return new Process();
        }

        public ProcessPriorityClass PriorityClass 
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public IntPtr ProcessorAffinity
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
#endif