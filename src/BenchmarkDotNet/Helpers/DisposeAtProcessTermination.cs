using System;

namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// Ensures that Dispose is called at termination of the Process.
    /// </summary>
    public class DisposeAtProcessTermination : IDisposable
    {
        public DisposeAtProcessTermination()
        {
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            // It does not make sense to include a finalizer. As we are subscribed to static events,
            // it will never be called.
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
