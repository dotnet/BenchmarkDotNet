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
        private IntPtr consoleHandle = IntPtr.Zero;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        private const int STD_OUTPUT_HANDLE = -11;

        internal TaskbarProgress()
        {
            if (OsVersionIsSupported)
            {
                consoleWindowHandle = GetConsoleWindow();
                consoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                Console.CancelKeyPress += OnConsoleCancelEvent;
            }
        }

        internal void SetState(TaskbarProgressState state)
        {
            if (OsVersionIsSupported)
            {
                TaskbarProgressCom.SetState(consoleWindowHandle, consoleHandle, state);
            }
        }

        internal void SetProgress(float progressValue)
        {
            if (OsVersionIsSupported)
            {
                TaskbarProgressCom.SetValue(consoleWindowHandle, consoleHandle, progressValue);
            }
        }

        private void OnConsoleCancelEvent(object sender, ConsoleCancelEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (OsVersionIsSupported)
            {
                TaskbarProgressCom.SetState(consoleWindowHandle, consoleHandle, TaskbarProgressState.NoProgress);
                consoleWindowHandle = IntPtr.Zero;
                consoleHandle = IntPtr.Zero;
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
        Paused = 0x8,
        Warning = Paused
    }

    internal static class TaskbarProgressCom
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out ConsoleModes lpMode);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, ConsoleModes dwMode);

        [Flags]
        private enum ConsoleModes : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,

            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }

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

        internal static void SetState(IntPtr consoleWindowHandle, IntPtr consoleHandle, TaskbarProgressState taskbarState)
        {
            if (consoleWindowHandle != IntPtr.Zero)
            {
                s_taskbarInstance.SetProgressState(consoleWindowHandle, taskbarState);
            }

            if (consoleHandle != IntPtr.Zero)
            {
                // Write progress state to console for Windows Terminal (https://github.com/microsoft/terminal/issues/6700).
                GetConsoleMode(consoleHandle, out ConsoleModes previousConsoleMode);
                SetConsoleMode(consoleHandle, ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING | ConsoleModes.ENABLE_PROCESSED_OUTPUT);
                switch (taskbarState)
                {
                    case TaskbarProgressState.NoProgress:
                        Console.Write("\x1b]9;4;0;0\x1b\\");
                        break;
                    case TaskbarProgressState.Indeterminate:
                        Console.Write("\x1b]9;4;3;0\x1b\\");
                        break;
                    case TaskbarProgressState.Normal:
                        // Do nothing, this is set automatically when SetValue is called (and WT has no documented way to set this).
                        break;
                    case TaskbarProgressState.Error:
                        Console.Write("\x1b]9;4;2;0\x1b\\");
                        break;
                    case TaskbarProgressState.Warning:
                        Console.Write("\x1b]9;4;4;0\x1b\\");
                        break;
                }
                SetConsoleMode(consoleHandle, previousConsoleMode);
            }
        }

        internal static void SetValue(IntPtr consoleWindowHandle, IntPtr consoleHandle, float progressValue)
        {
            bool isValidRange = progressValue >= 0 & progressValue <= 1;
            if (!isValidRange)
            {
                throw new ArgumentOutOfRangeException(nameof(progressValue), "progressValue must be between 0 and 1 inclusive.");
            }
            uint value = (uint) (progressValue * 100);

            if (consoleWindowHandle != IntPtr.Zero)
            {
                s_taskbarInstance.SetProgressValue(consoleWindowHandle, value, 100);
            }

            if (consoleHandle != IntPtr.Zero)
            {
                // Write progress sequence to console for Windows Terminal (https://github.com/microsoft/terminal/discussions/14268).
                GetConsoleMode(consoleHandle, out ConsoleModes previousConsoleMode);
                SetConsoleMode(consoleHandle, ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING | ConsoleModes.ENABLE_PROCESSED_OUTPUT);
                Console.Write($"\x1b]9;4;1;{value}\x1b\\");
                SetConsoleMode(consoleHandle, previousConsoleMode);
            }
        }
    }
}
