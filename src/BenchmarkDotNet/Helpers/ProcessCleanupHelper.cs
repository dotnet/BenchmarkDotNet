using System.Diagnostics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Helpers
{
    internal class ProcessCleanupHelper : DisposeAtProcessTermination
    {
        private readonly Process process;
        private readonly ILogger logger;

        internal ProcessCleanupHelper(Process process, ILogger logger)
        {
            this.process = process;
            this.logger = logger;
        }

        protected override void Dispose(bool exiting)
        {
            if (exiting)
            {
                KillProcessTree();
            }
            base.Dispose(exiting);
        }

        internal void KillProcessTree()
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