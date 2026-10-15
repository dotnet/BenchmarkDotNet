using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Running
{
    /// <summary>
    /// Called by the code that BenchmarkDotNet's source generator adds to a project published with Native AOT
    /// (<c>PublishAot</c> or <c>PublishTrimmed</c>). It is not meant to be called directly.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class NativeAotRegistration
    {
        /// <summary>
        /// Lets the in-process toolchain run methods returning <typeparamref name="T"/>, <c>ref T</c>,
        /// <see cref="ValueTask{TResult}"/> of <typeparamref name="T"/> or <see cref="IAsyncEnumerable{T}"/> of
        /// <typeparamref name="T"/> in an application that cannot generate code.
        /// </summary>
        public static void RegisterResultType<T>() => AotBenchmarkActions.Register<T>();

        /// <summary>
        /// Keeps the members of the benchmark type <typeparamref name="T"/>, which BenchmarkDotNet discovers by reflection,
        /// and lets the in-process toolchain create it without reflection, which a generic type closed over a value type needs
        /// in an application that cannot generate code.
        /// </summary>
        public static void RegisterBenchmarkType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : new()
            => BenchmarkInstanceFactory.Register(typeof(T), static () => new T());
    }
}
