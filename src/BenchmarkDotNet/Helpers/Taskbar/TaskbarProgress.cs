using System;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Helpers
{
    internal class TaskbarProgress : IDisposable
    {
        private static readonly bool OsVersionIsSupported = Portability.RuntimeInformation.IsWindows()
            // Must be windows 7 or greater
            && Environment.OSVersion.Version >= new Version(6, 1);

        private IntPtr consoleWindowHandle = IntPtr.Zero;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        internal TaskbarProgress()
        {
            if (OsVersionIsSupported)
            {
                consoleWindowHandle = GetConsoleWindow();
                if (consoleWindowHandle != IntPtr.Zero)
                {
                    TaskbarProgressCom.SetState(consoleWindowHandle, TaskbarProgressState.Normal);
                    Console.CancelKeyPress += OnConsoleCancelEvent;
                }
            }
        }

        internal void SetProgress(ulong currentValue, ulong maximumValue)
        {
            if (consoleWindowHandle != IntPtr.Zero)
            {
                TaskbarProgressCom.SetValue(consoleWindowHandle, currentValue, maximumValue);
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
                TaskbarProgressCom.SetState(consoleWindowHandle, TaskbarProgressState.NoProgress);
                consoleWindowHandle = IntPtr.Zero;
                Console.CancelKeyPress -= OnConsoleCancelEvent;
            }
        }
    }

    internal enum TaskbarProgressState
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }

    internal static class TaskbarProgressCom
    {
        [ComImport]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            // ITaskbarList
            [PreserveSig]
            void HrInit();
            [PreserveSig]
            void AddTab(IntPtr hwnd);
            [PreserveSig]
            void DeleteTab(IntPtr hwnd);
            [PreserveSig]
            void ActivateTab(IntPtr hwnd);
            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            [PreserveSig]
            void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
            [PreserveSig]
            void SetProgressState(IntPtr hwnd, TaskbarProgressState state);
        }

        [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
        [ClassInterface(ClassInterfaceType.None)]
        [ComImport]
        private class TaskbarInstance
        {
        }

        private static readonly ITaskbarList3 s_taskbarInstance = (ITaskbarList3) new TaskbarInstance();

        internal static void SetState(IntPtr windowHandle, TaskbarProgressState taskbarState)
        {
            s_taskbarInstance.SetProgressState(windowHandle, taskbarState);
        }

        internal static void SetValue(IntPtr windowHandle, ulong progressValue, ulong progressMax)
        {
            s_taskbarInstance.SetProgressValue(windowHandle, progressValue, progressMax);
        }
    }
}
