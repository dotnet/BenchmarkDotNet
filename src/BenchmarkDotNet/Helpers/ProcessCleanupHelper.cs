using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Helpers
{
    internal class ProcessCleanupHelper : DisposeAtProcessTermination, IAsyncDisposable
    {
        private readonly Process process;
        private readonly AsyncProcessOutputReader? outputReader;
        private readonly ILogger logger;

        internal ProcessCleanupHelper(Process process, ILogger logger)
            : this(process, outputReader: null, logger) { }

        internal ProcessCleanupHelper(Process process, AsyncProcessOutputReader? outputReader, ILogger logger)
        {
            this.process = process;
            this.outputReader = outputReader;
            this.logger = logger;
        }

        protected override void Dispose(bool exiting)
        {
            if (exiting)
            {
                KillProcessTree();
                base.Dispose(exiting);
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    if (outputReader?.IsStarted == true)
                    {
                        outputReader.CancelRead();
                    }
                    KillProcessTree();
                }
                else if (outputReader?.IsStarted == true)
                {
                    outputReader.StopReadAsync().AsTask().GetAwaiter().GetResult();
                }
            }
            catch
            {
                // process.HasExited can throw if the process was never started; we don't care
            }
            base.Dispose(exiting);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!process.HasExited)
                {
                    if (outputReader?.IsStarted == true)
                    {
                        outputReader.CancelRead();
                    }
                    KillProcessTree();
                }
                else if (outputReader?.IsStarted == true)
                {
                    await outputReader.StopReadAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // we don't care about exceptions here, we just try to cleanup whatever we can
            }

            base.Dispose(false);
        }

        private void KillProcessTree()
        {
            try
            {
                logger.Flush(); // Save log to file as soon as possible. Without it, the file log will be empty if the process has already died.

                process.KillTree(); // we need to kill entire process tree, not just the process itself
            }
            catch
            {
                // we don't care about exceptions here, it's shutdown and we just try to cleanup whatever we can
            }
        }
    }
}
