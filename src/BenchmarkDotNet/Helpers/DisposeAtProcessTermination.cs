using BenchmarkDotNet.Detectors;
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
    /// Usage: Derive your class that changes system state of this class. Revert system state in
    /// override of <see cref="Dispose(bool)"/> implementation.
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
        // Cancel key presses are not supported by .NET or do not exist for these platforms.
        internal static readonly bool ConsoleSupportsCancelKeyPress =
            !(OsDetector.IsAndroid() || OsDetector.IsIOS() || OsDetector.IsTvOS() || Portability.RuntimeInformation.IsWasm);

        private static int cancelKeyPressKeepAliveRegistered;

        private int disposed;

        // #3181
        // Called by every site that subscribes to Console.CancelKeyPress and later unsubscribes, before subscribing.
        // It installs a single no-op handler that is never removed, so the CancelKeyPress handler count can never
        // drop to zero. Removing the last handler calls SetConsoleCtrlHandler(..., Add: false), which deadlocks
        // in Windows when the user presses ctrl+c multiple times rapidly before the first event execution completed.
        internal static void EnsureCancelKeyPressKeepAlive()
        {
            if (ConsoleSupportsCancelKeyPress && Interlocked.Exchange(ref cancelKeyPressKeepAliveRegistered, 1) == 0)
            {
                Console.CancelKeyPress += static (_, _) => { };
            }
        }

        protected DisposeAtProcessTermination()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            if (ConsoleSupportsCancelKeyPress)
            {
                EnsureCancelKeyPressKeepAlive();
                Console.CancelKeyPress += OnCancelKeyPress;
            }

            // It does not make sense to include a Finalizer. We do not manage any native resource and:
            // as we are subscribed to static events, it would never be called.
        }

        // Ensures the disposal logic runs at most once. The same instance can be disposed both explicitly
        // (using/await using) and via the OnCancelKeyPress / OnProcessExit handlers when a run is aborted.
        // Without this guard, subclasses would run their cleanup twice - e.g. WakeLockSentinel would call
        // PowerClearRequest on an already-disposed SafeHandle and throw ObjectDisposedException.
        protected bool MarkDisposed()
            => Interlocked.Exchange(ref disposed, 1) == 0;

        /// <summary>
        /// Called when the user presses Ctrl-C or Ctrl-Break.
        /// </summary>
        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            if (!e.Cancel && MarkDisposed())
            {
                Dispose(true);
            }
        }

        /// <summary>
        /// Called when the user clicks on the X in the upper right corner to close the Benchmark's Window.
        /// </summary>
        private void OnProcessExit(object? sender, EventArgs e)
        {
            if (MarkDisposed())
            {
                Dispose(true);
            }
        }

        public void Dispose()
        {
            if (MarkDisposed())
            {
                Dispose(false);
            }
        }

        protected virtual void Dispose(bool exiting)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            if (ConsoleSupportsCancelKeyPress)
            {
                Console.CancelKeyPress -= OnCancelKeyPress;
            }
        }
    }
}
