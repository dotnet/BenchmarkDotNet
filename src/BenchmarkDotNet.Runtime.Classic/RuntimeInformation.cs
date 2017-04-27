using System;

namespace BenchmarkDotNet
{
    public class RuntimeInformation
    {
        private static readonly bool isMono =
            Type.GetType("Mono.Runtime") != null; // it allocates a lot of memory, we need to check it once in order to keep Enging non-allocating!

        public static readonly Portability.RuntimeInformation Instance = 
            isMono 
                ? (Portability.RuntimeInformation)new MonoRuntimeInformation() 
                : new DesktopDotNetFrameworkRuntimeInformation();
    }
}