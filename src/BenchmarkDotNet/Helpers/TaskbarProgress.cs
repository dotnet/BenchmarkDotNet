using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Detectors;

namespace BenchmarkDotNet.Helpers
{
    internal enum TaskbarProgressState
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8,
        Warning = Paused
    }

    internal class TaskbarProgress : DisposeAtProcessTermination
    {
        private static readonly bool OsVersionIsSupported = OsDetector.IsWindows()
            // Must be windows 7 or greater
            && Environment.OSVersion.Version >= new Version(6, 1);

        private Com? com;
        private Terminal? terminal;

        private bool IsEnabled => com != null || terminal != null;

        internal TaskbarProgress(TaskbarProgressState initialTaskbarState)
        {
            if (OsVersionIsSupported)
            {
                com = Com.MaybeCreateInstanceAndSetInitialState(initialTaskbarState);
                terminal = Terminal.MaybeCreateInstanceAndSetInitialState(initialTaskbarState);
            }
        }

        internal void SetState(TaskbarProgressState state)
        {
            com?.SetState(state);
            terminal?.SetState(state);
        }

        internal void SetProgress(float progressValue)
        {
            bool isValidRange = progressValue >= 0 & progressValue <= 1;
            if (!isValidRange)
            {
                throw new ArgumentOutOfRangeException(nameof(progressValue), "progressValue must be between 0 and 1 inclusive.");
            }
            uint value = (uint)(progressValue * 100);
            com?.SetValue(value);
            terminal?.SetValue(value);
        }

        public override void Dispose()
        {
            if (IsEnabled)
            {
                SetState(TaskbarProgressState.NoProgress);
                com = null;
                terminal = null;
            }
            base.Dispose();
        }

        private sealed class Com
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
            private class TaskbarInstance { }

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetConsoleWindow();

            private readonly ITaskbarList3 taskbarInstance;
            private readonly IntPtr consoleWindowHandle;

            private Com(IntPtr handle)
            {
                taskbarInstance = (ITaskbarList3) new TaskbarInstance();
                consoleWindowHandle = handle;
            }

            internal static Com? MaybeCreateInstanceAndSetInitialState(TaskbarProgressState initialTaskbarState)
            {
                try
                {
                    IntPtr handle = GetConsoleWindow();
                    if (handle == IntPtr.Zero)
                    {
                        return null;
                    }
                    var com = new Com(handle);
                    com.SetState(initialTaskbarState);
                    return com;
                }
                // COM may be disabled, in which case this will throw (#2253).
                // It could be NotSupportedException or COMException, we just catch all.
                catch
                {
                    return null;
                }
            }

            internal void SetState(TaskbarProgressState taskbarState)
                => taskbarInstance.SetProgressState(consoleWindowHandle, taskbarState);

            /// <summary>
            /// Sets the progress value out of 100.
            /// </summary>
            internal void SetValue(uint progressValue)
                => taskbarInstance.SetProgressValue(consoleWindowHandle, progressValue, 100);
        }

        private sealed class Terminal
        {
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

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out ConsoleModes lpMode);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetConsoleMode(IntPtr hConsoleHandle, ConsoleModes dwMode);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr GetStdHandle(int nStdHandle);
            private const int STD_OUTPUT_HANDLE = -11;

            private readonly IntPtr consoleHandle;
            private uint currentProgress;

            private Terminal(IntPtr handle)
                => consoleHandle = handle;

            internal static Terminal? MaybeCreateInstanceAndSetInitialState(TaskbarProgressState initialTaskbarState)
            {
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (handle == IntPtr.Zero)
                {
                    return null;
                }
                if (!GetConsoleMode(handle, out ConsoleModes previousConsoleMode)
                    || !SetConsoleMode(handle, ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING | ConsoleModes.ENABLE_PROCESSED_OUTPUT))
                {
                    // If we failed to set virtual terminal processing mode, it is likely due to an older Windows version that does not support it,
                    // or legacy console. In either case the TaskbarProgressCom will take care of the progress. See https://stackoverflow.com/a/44574463/5703407.
                    // If we try to write without VT mode, the sequence will be printed for the user to see, which clutters the output.
                    return null;
                }
                var terminal = new Terminal(handle);
                terminal.WriteStateSequence(initialTaskbarState);
                SetConsoleMode(handle, previousConsoleMode);
                return terminal;
            }

            internal void SetState(TaskbarProgressState taskbarState)
            {
                GetConsoleMode(consoleHandle, out ConsoleModes previousConsoleMode);
                SetConsoleMode(consoleHandle, ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING | ConsoleModes.ENABLE_PROCESSED_OUTPUT);
                WriteStateSequence(taskbarState);
                SetConsoleMode(consoleHandle, previousConsoleMode);
            }

            private void WriteStateSequence(TaskbarProgressState taskbarState)
            {
                // Write progress state to console for Windows Terminal (https://github.com/microsoft/terminal/issues/6700).
                switch (taskbarState)
                {
                    case TaskbarProgressState.NoProgress:
                        currentProgress = 100;
                        Console.Write("\x1b]9;4;0;0\x1b\\");
                        break;
                    case TaskbarProgressState.Indeterminate:
                        currentProgress = 100;
                        Console.Write("\x1b]9;4;3;0\x1b\\");
                        break;
                    case TaskbarProgressState.Normal:
                        // Normal state is set when progress is set.
                        WriteProgressSequence(currentProgress);
                        break;
                    case TaskbarProgressState.Error:
                        Console.Write($"\x1b]9;4;2;{currentProgress}\x1b\\");
                        break;
                    case TaskbarProgressState.Warning:
                        Console.Write($"\x1b]9;4;4;{currentProgress}\x1b\\");
                        break;
                }
            }

            /// <summary>
            /// Sets the progress value out of 100.
            /// </summary>
            internal void SetValue(uint progressValue)
            {
                currentProgress = progressValue;
                // Write progress sequence to console for Windows Terminal (https://github.com/microsoft/terminal/discussions/14268).
                GetConsoleMode(consoleHandle, out ConsoleModes previousConsoleMode);
                SetConsoleMode(consoleHandle, ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING | ConsoleModes.ENABLE_PROCESSED_OUTPUT);
                WriteProgressSequence(progressValue);
                SetConsoleMode(consoleHandle, previousConsoleMode);
            }

            private static void WriteProgressSequence(uint progressValue)
                => Console.Write($"\x1b]9;4;1;{progressValue}\x1b\\");
        }
    }
}
