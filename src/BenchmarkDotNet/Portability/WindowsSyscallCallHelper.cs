using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Portability
{
    internal static class WindowsSyscallCallHelper
    {
        // Before https://github.com/dotnet/runtime/pull/54676 (.NET 6)
        // .NET was not allowing to open named pipes using FileStream(path)
        // So here we go, calling sys-call directly...
        internal static FileStream OpenNamedPipe(string namePipePath, FileAccess fileAccess)
        {
            // copied from https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/System.IO.Pipes/src/System/IO/Pipes/NamedPipeClientStream.Windows.cs#L23-L45
            int access = fileAccess == FileAccess.Read ? unchecked((int)0x80000000) : 0x40000000; // Generic Read & Write
            SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(HandleInheritability.None);

            SafeFileHandle sfh = CreateFileW(namePipePath, access, FileShare.None, ref secAttrs, FileMode.Open, 0, hTemplateFile: IntPtr.Zero);
            if (sfh.IsInvalid)
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Exception($"Unable to open Named Pipe {namePipePath}. Last error: {lastError:X}");
            }

            return new FileStream(sfh, fileAccess);

            [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true)]
            static extern SafeFileHandle CreateFileW(
                [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
                int dwDesiredAccess,
                FileShare dwShareMode,
                ref SECURITY_ATTRIBUTES secAttrs,
                FileMode dwCreationDisposition,
                int dwFlagsAndAttributes,
                IntPtr hTemplateFile);
        }

        // copied from https://github.com/dotnet/runtime/blob/c59b5171e4fb2b000108bec965f8ce443cb95a12/src/libraries/System.IO.Pipes/src/System/IO/Pipes/PipeStream.Windows.cs#L568
        private static unsafe SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
        {
            SECURITY_ATTRIBUTES secAttrs = new SECURITY_ATTRIBUTES
            {
                nLength = (uint)sizeof(SECURITY_ATTRIBUTES),
                bInheritHandle = ((inheritability & HandleInheritability.Inheritable) != 0) ? BOOL.TRUE : BOOL.FALSE
            };

            return secAttrs;
        }

        // copied from https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/Common/src/Interop/Windows/Interop.BOOL.cs
        internal enum BOOL : int
        {
            FALSE = 0,
            TRUE = 1,
        }

        // copied from https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/Common/src/Interop/Windows/Kernel32/Interop.SECURITY_ATTRIBUTES.cs
        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            internal uint nLength;
            internal IntPtr lpSecurityDescriptor;
            internal BOOL bInheritHandle;
        }
    }
}
