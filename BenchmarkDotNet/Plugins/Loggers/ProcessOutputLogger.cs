using System;
using System.Diagnostics;

namespace BenchmarkDotNet.Plugins.Loggers
{
    internal class ProcessOutputLogger : IDisposable
    {
        protected readonly IBenchmarkLogger logger;
        protected readonly Process process;

        public ProcessOutputLogger(IBenchmarkLogger logger, Process process)
        {
            if (process.StartInfo.UseShellExecute)
            {
                throw new NotSupportedException("set UseShellExecute to false first");
            }
            if (!process.StartInfo.RedirectStandardOutput)
            {
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            }
            if (!process.StartInfo.RedirectStandardError)
            {
                throw new NotSupportedException("set RedirectStandardError to true first");
            }

            this.logger = logger;
            this.process = process;

            this.process.OutputDataReceived += ProcessOnOutputDataReceived;
            this.process.ErrorDataReceived += ProcessOnErrorDataReceived;
        }

        public void Dispose()
        {
            process.OutputDataReceived -= ProcessOnOutputDataReceived;
            process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        }

        protected virtual void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            logger.WriteLine(BenchmarkLogKind.Default, dataReceivedEventArgs.Data);
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            logger.WriteLine(BenchmarkLogKind.Error, dataReceivedEventArgs.Data);
        }
    }
}