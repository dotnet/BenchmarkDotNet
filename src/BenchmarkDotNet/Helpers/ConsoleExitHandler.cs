using System;
using System.Diagnostics;
using System.Threading;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Helpers
{
    internal class ConsoleExitHandler : IDisposable
    {
        private readonly Process process;
        private readonly ILogger logger;

        internal ConsoleExitHandler(Process process, ILogger logger)
        {
            this.process = process;
            this.logger = logger;

            Console.CancelKeyPress += CancelKeyPressHandlerCallback;
            AppDomain.CurrentDomain.ProcessExit += ProcessExitEventHandlerHandlerCallback;
            NativeWindowsConsoleHelper.OnExit += CancelKeyPressHandlerCallback;
        }

        public void Dispose()
        {
            NativeWindowsConsoleHelper.OnExit -= CancelKeyPressHandlerCallback;
            AppDomain.CurrentDomain.ProcessExit -= ProcessExitEventHandlerHandlerCallback;
            Console.CancelKeyPress -= CancelKeyPressHandlerCallback;
        }

        private void CancelKeyPressHandlerCallback(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();

            KillProcess();
        }

        private void ProcessExitEventHandlerHandlerCallback(object sender, EventArgs e) => KillProcess();

        // This method gives us a chance to make a "best-effort" to clean anything up after Ctrl-C is type in the Console
        private void KillProcess()
        {
            try
            {
                //Save log to file as soon as possible. Without it, the file log will be empty if the process has already died.
                logger.Flush();

                if (HasProcessDied(process))
                    return;

                logger.WriteLineError($"Process {process.ProcessName}.exe (Id:{process.Id}) is still running, will now be killed with the entire process tree");
                logger.Flush();

                process.KillTree(); // we need to kill entire process tree, not just the process itself

                if (HasProcessDied(process))
                    return;

                // Give it a bit of time to exit!
                Thread.Sleep(500);

                if (HasProcessDied(process))
                    return;

                var matchingProcess = Process.GetProcessById(process.Id);
                if (HasProcessDied(matchingProcess) || HasProcessDied(process))
                    return;

                logger.WriteLineError($"Process {matchingProcess.ProcessName}.exe (Id:{matchingProcess.Id}) has not exited after being killed!");
            }
            catch (ArgumentException)
            {
                // the process has died in the meantime, don't log the exception
            }
            catch (InvalidOperationException invalidOpEx)
            {
                logger.WriteLineError(invalidOpEx.Message);
            }
            catch (Exception ex)
            {
                logger.WriteLineError(ex.ToString());
            }

            logger.Flush();
        }

        private static bool HasProcessDied(Process process)
        {
            if (process == null)
                return true;
            try
            {
                return process.HasExited; // This can throw an exception
            }
            catch (Exception)
            {
                // Swallow!!
            }
            return true;
        }
    }
}