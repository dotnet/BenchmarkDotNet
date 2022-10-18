using Microsoft.WindowsAPICodePack.Taskbar;
using System;

namespace BenchmarkDotNet.Helpers
{
    internal class TaskbarProgress : IDisposable
    {
        private IntPtr consoleWindowHandle = IntPtr.Zero;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        internal TaskbarProgress()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                consoleWindowHandle = GetConsoleWindow();
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, consoleWindowHandle);
                Console.CancelKeyPress += OnConsoleCancelEvent;
            }
        }

        internal void SetProgress(int currentValue, int maximumValue)
        {
            if (consoleWindowHandle != IntPtr.Zero)
            {
                TaskbarManager.Instance.SetProgressValue(currentValue, maximumValue, consoleWindowHandle);
            }
        }

        private void OnConsoleCancelEvent(object sender, ConsoleCancelEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (consoleWindowHandle != IntPtr.Zero)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, consoleWindowHandle);
                consoleWindowHandle = IntPtr.Zero;
                Console.CancelKeyPress -= OnConsoleCancelEvent;
            }
        }
    }
}
