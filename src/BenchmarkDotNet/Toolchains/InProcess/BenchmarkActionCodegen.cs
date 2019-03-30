using System;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    ///     How benchmark action code is generated
    /// </summary>
    [Obsolete("Please use BenchmarkDotNet.Toolchains.InProcess.NoEmit.* classes")]
    public enum BenchmarkActionCodegen
    {
        /// <summary>
        ///     The unroll feature is implemented using dynamic method codegen (Reflection.Emit).
        ///     Provides most accurate results but may not work as expected on some platforms (e.g. .Net Native).
        /// </summary>
        ReflectionEmit,

        /// <summary>
        ///     Fallback option: the unroll feature is implemented using
        ///     <see cref="Delegate.Combine(System.Delegate,System.Delegate)"/> method.
        ///     Has additional overhead (+1 delegate call) but should work on all platforms.
        /// </summary>
        DelegateCombine
    }
}