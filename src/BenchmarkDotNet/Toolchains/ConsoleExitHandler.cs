using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Toolchains
{
    internal class ConsoleExitHandler
    {
        // This needs to be singleton, so that we can share a single handler amongst all instances of Executor's
        internal static Lazy<ConsoleExitHandler> InstanceLazy = new Lazy<ConsoleExitHandler>(
            () => new ConsoleExitHandler(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        public static ConsoleExitHandler Instance { get { return InstanceLazy.Value; } }

        public Process Process { get; set; }
        public ILogger Logger { get; set; }

        private ConsoleExitHandler()
        {
            Console.CancelKeyPress += CancelKeyPressHandlerCallback;
            AppDomain.CurrentDomain.ProcessExit += ProcessExitEventHandlerHandlerCallback;
        }

        private void CancelKeyPressHandlerCallback(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();

            if (e.SpecialKey != ConsoleSpecialKey.ControlC && e.SpecialKey != ConsoleSpecialKey.ControlBreak)
                return;

            KillProcess();
        }

        private void ProcessExitEventHandlerHandlerCallback(object sender, EventArgs e)
        {
            KillProcess();
        }

        // This method gives us a chance to make a "best-effort" to clean anything up after Ctrl-C is type in the Console
        private void KillProcess()
        {
            try
            {
                //Save log to file as soon as possible. Without it, the file log will be empty if the process has already died.
                Logger?.Flush();

                // Take a copy, in case SetProcess(..) is called whilst we are executing!
                var localProcess = Process;

                if (HasProcessDied(localProcess))
                    return;

                Logger?.WriteLineError($"Process {localProcess.ProcessName}.exe (Id:{localProcess.Id}) is still running, will now be killed");
                Logger?.Flush();

                localProcess.Kill();

                if (HasProcessDied(localProcess))
                    return;

                // Give it a bit of time to exit!
                Thread.Sleep(500);

                if (HasProcessDied(localProcess))
                    return;

                var matchingProcess = Process.GetProcesses().FirstOrDefault(p => p.Id == localProcess.Id);
                if (matchingProcess == null || HasProcessDied(matchingProcess) || HasProcessDied(localProcess))
                    return;
                Logger?.WriteLineError($"Process {matchingProcess.ProcessName}.exe (Id:{matchingProcess.Id}) has not exited after being killed!");
            }
            catch (InvalidOperationException invalidOpEx)
            {
                Logger?.WriteLineError(invalidOpEx.Message);
            }
            catch (Exception ex)
            {
                Logger?.WriteLineError(ex.ToString());
            }

            Logger?.Flush();
        }

        public override string ToString()
        {
            int? processId = -1;
            try
            {
                processId = Process?.Id;
            }
            catch (Exception)
            {
                // Swallow!!
            }
            return $"Process: {processId}";
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