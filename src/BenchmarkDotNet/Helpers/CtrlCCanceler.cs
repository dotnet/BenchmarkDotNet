using BenchmarkDotNet.Loggers;
using System;
using System.Threading;

namespace BenchmarkDotNet.Helpers
{
    internal sealed class CtrlCCanceler : IDisposable
    {
        private int cancelled;
        private readonly CancellationTokenSource cts;
        private readonly ILogger logger;

        private CtrlCCanceler(CancellationTokenSource cts, ILogger logger)
        {
            this.logger = logger;
            this.cts = cts;
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        public static CtrlCCanceler? Create(ref CancellationToken cancellationToken, ILogger logger)
        {
            if (!DisposeAtProcessTermination.ConsoleSupportsCancelKeyPress)
            {
                return null;
            }
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationToken = cts.Token;
            return new(cts, logger);
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            if (Interlocked.Exchange(ref cancelled, 1) == 1)
            {
                return;
            }

            Console.CancelKeyPress -= OnCancelKeyPress;
            e.Cancel = true;
            logger.WriteLineInfo("Cancelling benchmark run... press ctrl+c again to force abort.");
            cts.Cancel();
        }

        public void Dispose()
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            cts.Dispose();
        }
    }
}
