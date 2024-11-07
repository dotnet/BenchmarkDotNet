using System;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// Ensures that explicit Dispose is called at termination of the Process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class exists to help in reverting system state where C#'s using statement does not
    /// suffice. I.e. when Benchmark's process is aborted via Ctrl-C, Ctrl-Break or via click on the
    /// X in the upper right of Window.
    /// </para>
    /// <para>
    /// Usage: Derive your clas that changes system state of this class. Revert system state in
    /// override of <see cref="Dispose"/> implementation.
    /// Use your class in C#'s using statement, to ensure system state is reverted in normal situations.
    /// This class ensures your override is also called at process 'abort'.
    /// </para>
    /// <para>
    /// Note: This class is explicitly not responsible for cleanup of Native resources. Of course,
    /// derived classes can cleanup their Native resources (usually managed via
    /// <see cref="SafeHandle"/> derived classes), by delegating explicit Disposal to their
    /// <see cref="IDisposable"/> fields.
    /// </para>
    /// </remarks>
    public abstract class DisposeAtProcessTermination : IDisposable
    {
        public DisposeAtProcessTermination()
        {
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            // It does not make sense to include a Finalizer. We do not manage any native resource and:
            // as we are subscribed to static events, it would never be called.
        }

        /// <summary>
        /// Called when the user presses Ctrl-C or Ctrl-Break.
        /// </summary>
        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            if (!e.Cancel) { Dispose(); }
        }

        /// <summary>
        /// Called when the user clicks on the X in the upper right corner to close the Benchmark's Window.
        /// </summary>
        private void OnProcessExit(object? sender, EventArgs e) => Dispose();

        public virtual void Dispose()
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }
    }
}
