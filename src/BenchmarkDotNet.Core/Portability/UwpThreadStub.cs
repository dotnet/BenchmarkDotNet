#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading
{
    public delegate void ThreadStart();

    public class Thread
    {
        public Thread(ThreadStart start)
        {
            throw new PlatformNotSupportedException();
        }

        public bool IsBackground 
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
        
        public void Start()
        {
            throw new PlatformNotSupportedException();
        }

        
        public bool Join(int millisecondsTimeout)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
#endif