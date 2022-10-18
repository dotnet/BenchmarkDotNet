//Copyright (c) Microsoft Corporation.  All rights reserved.

using MS.WindowsAPICodePack.Internal;
using System;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Represents an instance of the Windows taskbar
    /// </summary>
    public class TaskbarManager
    {
        // Hide the default constructor
        private TaskbarManager() => CoreHelpers.ThrowIfNotWin7OrLater();

        // Best practice recommends defining a private object to lock on
        private static readonly object _syncLock = new object();

        private static TaskbarManager _instance;
        /// <summary>
        /// Represents an instance of the Windows Taskbar
        /// </summary>
        public static TaskbarManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TaskbarManager();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Displays or updates a progress bar hosted in a taskbar button of the given window handle
        /// to show the specific percentage completed of the full operation.
        /// </summary>
        /// <param name="windowHandle">The handle of the window whose associated taskbar button is being used as a progress indicator.
        /// This window belong to a calling process associated with the button's application and must be already loaded.</param>
        /// <param name="currentValue">An application-defined value that indicates the proportion of the operation that has been completed at the time the method is called.</param>
        /// <param name="maximumValue">An application-defined value that specifies the value currentValue will have when the operation is complete.</param>
        public void SetProgressValue(int currentValue, int maximumValue, IntPtr windowHandle) => TaskbarList.Instance.SetProgressValue(
                windowHandle,
                Convert.ToUInt32(currentValue),
                Convert.ToUInt32(maximumValue));

        /// <summary>
        /// Sets the type and state of the progress indicator displayed on a taskbar button
        /// of the given window handle
        /// </summary>
        /// <param name="windowHandle">The handle of the window whose associated taskbar button is being used as a progress indicator.
        /// This window belong to a calling process associated with the button's application and must be already loaded.</param>
        /// <param name="state">Progress state of the progress button</param>
        public void SetProgressState(TaskbarProgressBarState state, IntPtr windowHandle) => TaskbarList.Instance.SetProgressState(windowHandle, (TaskbarProgressBarStatus)state);

        /// <summary>
        /// Indicates whether this feature is supported on the current platform.
        /// </summary>
        public static bool IsPlatformSupported =>
                // We need Windows 7 onwards ...
                CoreHelpers.RunningOnWin7OrLater;
    }
}
