using System;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Helpers
{
    // we need this class because when the Console window gets closed (by simply pressing x)
    // the managed event is not raised for CTRL_CLOSE_EVENT, more https://stackoverflow.com/questions/474679/capture-console-exit-c-sharp
    internal static class NativeWindowsConsoleHelper
    {
        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        
        private delegate bool ConsoleCtrlHandler(CtrlType sig);
        
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

        private static event EventHandler<ConsoleCancelEventArgs> onExit;

        public static event EventHandler<ConsoleCancelEventArgs> OnExit
        {
            add
            {
                if (Portability.RuntimeInformation.IsWindows())
                {
                    SetConsoleCtrlHandler(NativeHandler, true);

                    onExit += value;
                }
            }
            remove
            {
                if (Portability.RuntimeInformation.IsWindows())
                {
                    SetConsoleCtrlHandler(NativeHandler, false);
                    
                    onExit -= value;
                }
            }
        }
        
        private static bool NativeHandler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    onExit?.Invoke(null, null);
                    return false; // if we return true from here, the event is marked as handled and the managed handler is not fired
                default:
                    return false;
            }
        }
    }
}