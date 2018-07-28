using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    internal class ConsoleHandler
    {
        // This needs to be static, so that we can share a single handler amongst all instances of Executor's
        internal static ConsoleHandler Instance;

        [PublicAPI] public ConsoleCancelEventHandler EventHandler { get; }

        private Process process;
        private readonly ILogger logger;
        private ConsoleColor? colorBefore;

        [PublicAPI] public ConsoleHandler(ILogger logger)
        {
            this.logger = logger;
            EventHandler = HandlerCallback;
        }

        public static void EnsureInitialized(ILogger logger)
        {
            if (Instance == null)
            {
                Instance = new ConsoleHandler(logger);
                Console.CancelKeyPress += Instance.EventHandler;
            }
        }

        public void SetProcess(Process process)
        {
            this.process = process;
        }

        public void ClearProcess()
        {
            process = null;
        }

        // This method gives us a chance to make a "best-effort" to clean anything up after Ctrl-C is type in the Console
        private void HandlerCallback(object sender, ConsoleCancelEventArgs e)
        {
            Console.ResetColor();

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

        public static void SetForegroundColor(ConsoleColor color)
        {
            Instance.colorBefore = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public static void RestoreForegroundColor()
        {
            if (Instance.colorBefore.HasValue)
            {
                Console.ForegroundColor = Instance.colorBefore.Value;
                Instance.colorBefore = null;
            }
        }
    }
}