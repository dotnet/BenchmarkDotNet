using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
public static class DeadCodeEliminationHelper
{
    /// <summary>
    /// This method can't get inlined, so any value send to it
    /// will not get eliminated by the dead code elimination
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void KeepAliveWithoutBoxing<T>(T value)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    /// <summary>
    /// This method can't get inlined, so any value send to it
    /// will not get eliminated by the dead code elimination
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void KeepAliveWithoutBoxing<T>(in T value)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }
}