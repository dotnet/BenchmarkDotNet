using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Engines
{
    public static class DeadCodeEliminationHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void KeepAliveWithoutBoxing<T>(T value)
        {
            // this method can't get inlined, so any value send to it
            // will not get eliminated by the dead code elimination
        }
    }
}