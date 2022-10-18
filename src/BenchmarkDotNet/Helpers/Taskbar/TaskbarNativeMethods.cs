//Copyright (c) Microsoft Corporation.  All rights reserved.

using MS.WindowsAPICodePack.Internal;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    internal enum SetTabPropertiesOption
    {
        None = 0x0,
        UseAppThumbnailAlways = 0x1,
        UseAppThumbnailWhenActive = 0x2,
        UseAppPeekAlways = 0x4,
        UseAppPeekWhenActive = 0x8
    }

    internal enum ShellAddToRecentDocs
    {
        Pidl = 0x1,
        PathA = 0x2,
        PathW = 0x3,
        AppIdInfo = 0x4,       // indicates the data type is a pointer to a SHARDAPPIDINFO structure
        AppIdInfoIdList = 0x5, // indicates the data type is a pointer to a SHARDAPPIDINFOIDLIST structure
        Link = 0x6,            // indicates the data type is a pointer to an IShellLink instance
        AppIdInfoLink = 0x7,   // indicates the data type is a pointer to a SHARDAPPIDINFOLINK structure
    }

    internal enum TaskbarProgressBarStatus
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }

    internal enum ThumbButtonMask
    {
        Bitmap = 0x1,
        Icon = 0x2,
        Tooltip = 0x4,
        THB_FLAGS = 0x8
    }

    [Flags]
    internal enum ThumbButtonOptions
    {
        Enabled = 0x00000000,
        Disabled = 0x00000001,
        DismissOnClick = 0x00000002,
        NoBackground = 0x00000004,
        Hidden = 0x00000008,
        NonInteractive = 0x00000010
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct ThumbButton
    {
        /// <summary>WPARAM value for a THUMBBUTTON being clicked.</summary>
        internal const int Clicked = 0x1800;

        [MarshalAs(UnmanagedType.U4)]
        internal ThumbButtonMask Mask;

        internal uint Id;
        internal uint Bitmap;
        internal IntPtr Icon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string Tip;

        [MarshalAs(UnmanagedType.U4)]
        internal ThumbButtonOptions Flags;
    }

    internal static class TaskbarNativeMethods
    {
        internal const int WmCommand = 0x0111;

        internal const uint WmDwmSendIconicLivePreviewBitmap = 0x0326;

        internal const uint WmDwmSendIconThumbnail = 0x0323;

        // Register Window Message used by Shell to notify that the corresponding taskbar button has been added to the taskbar.
        internal static readonly uint WmTaskbarButtonCreated = RegisterWindowMessage("TaskbarButtonCreated");

        [DllImport("shell32.dll")]
        internal static extern void GetCurrentProcessExplicitAppUserModelID(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string AppID);

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessage", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint RegisterWindowMessage([MarshalAs(UnmanagedType.LPWStr)] string lpString);

        [DllImport("shell32.dll")]
        internal static extern void SetCurrentProcessExplicitAppUserModelID(
            [MarshalAs(UnmanagedType.LPWStr)] string AppID);

        [DllImport("shell32.dll")]
        internal static extern void SHAddToRecentDocs(
            ShellAddToRecentDocs flags,
            [MarshalAs(UnmanagedType.LPWStr)] string path);

        internal static void SHAddToRecentDocs(string path) => SHAddToRecentDocs(ShellAddToRecentDocs.PathW, path);

        internal static class TaskbarGuids
        {
            internal static Guid IObjectArray = new Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9");
            internal static Guid IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
        }
    }
}