using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Toolchains
{
    internal class ConsoleHandler
    {
        public ConsoleCancelEventHandler EventHandler { get; private set; }

        private Process process;
        private ILogger logger;

        public ConsoleHandler(ILogger logger)
        {
            this.logger = logger;
            EventHandler = new ConsoleCancelEventHandler(HandlerCallback);
        }

        public void SetProcess(Process process)
        {
            this.process = process;
        }

        public void ClearProcess()
        {
            this.process = null;
        }

        // This method gives us a chance to make a "best-effort" to clean anything up after Ctrl-C is type in the Console
        private void HandlerCallback(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey != ConsoleSpecialKey.ControlC && e.SpecialKey != ConsoleSpecialKey.ControlBreak)
                return;

            try
            {
                // Take a copy, in case SetProcess(..) is called whilst we are executing!
                var localProcess = process;

                if (HasProcessDied(localProcess))
                    return;

                logger?.WriteLineError($"Process {localProcess.ProcessName}.exe (Id:{localProcess.Id}) is still running, will now be killed");
                localProcess.Kill();

                if (HasProcessDied(localProcess))
                    return;

                // Give it a bit of time to exit!
                Thread.Sleep(500);

                if (HasProcessDied(localProcess))
                    return;

                var matchingProcess = Process.GetProcesses().FirstOrDefault(p => p.Id == localProcess.Id);
                if (HasProcessDied(matchingProcess) || HasProcessDied(localProcess))
                    return;
                logger?.WriteLineError($"Process {matchingProcess.ProcessName}.exe (Id:{matchingProcess.Id}) has not exited after being killed!");
            }
            catch (InvalidOperationException invalidOpEx)
            {
                logger?.WriteLineError(invalidOpEx.Message);
            }
            catch (Exception ex)
            {
                logger?.WriteLineError(ex.ToString());
            }
        }

        public override string ToString()
        {
            int? processId = -1;
            try
            {
                processId = process?.Id;
            }
            catch (Exception)
            {
                // Swallow!!
            }
            return $"Process: {processId}, Handler: {EventHandler?.GetHashCode()}";
        }

        private bool HasProcessDied(Process process)
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