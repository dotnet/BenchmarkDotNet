using System;
using System.Diagnostics;

namespace BenchmarkDotNet.Loggers
{
    internal class ProcessOutputLogger : IDisposable
    {
        protected readonly Process process;
        private readonly ILogger logger;

        public ProcessOutputLogger(ILogger logger, Process process)
        {
            if (process.StartInfo.UseShellExecute)
            {
                throw new NotSupportedException("set UseShellExecute to false first");
            }

            this.logger = logger;
            this.process = process;

            if (process.StartInfo.RedirectStandardOutput)
            {
                this.process.OutputDataReceived += ProcessOnOutputDataReceived;
            }
            if (process.StartInfo.RedirectStandardError)
            {
                this.process.ErrorDataReceived += ProcessOnErrorDataReceived;
            }
        }

        public void Dispose()
        {
            process.OutputDataReceived -= ProcessOnOutputDataReceived;
            process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        }

        protected virtual void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            logger.WriteLine(LogKind.Default, dataReceivedEventArgs.Data);
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            if (!string.IsNullOrEmpty(dataReceivedEventArgs.Data)) // happened often and added unnecessary blank line to output
            {
                logger.WriteLine(LogKind.Error, dataReceivedEventArgs.Data);
            }
        }
    }
}