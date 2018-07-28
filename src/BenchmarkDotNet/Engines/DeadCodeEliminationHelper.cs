using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public static class DeadCodeEliminationHelper
    {
        /// <summary>
        /// This method can't get inlined, so any value send to it
        /// will not get eliminated by the dead code elimination
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [UsedImplicitly] // Used in generated benchmarks
        public static void KeepAliveWithoutBoxing<T>(T value) { }

        /// <summary>
        /// This method can't get inlined, so any value send to it
        /// will not get eliminated by the dead code elimination
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [UsedImplicitly] // Used in generated benchmarks
        public static void KeepAliveWithoutBoxing<T>(ref T value) { }
    }
}