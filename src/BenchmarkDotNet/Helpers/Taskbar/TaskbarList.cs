namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Provides internal access to the functions provided by the ITaskbarList4 interface,
    /// without being forced to refer to it through another singleton.
    /// </summary>
    internal static class TaskbarList
    {
        private static readonly object _syncLock = new object();

        private static ITaskbarList4 _taskbarList;
        internal static ITaskbarList4 Instance
        {
            get
            {
                if (_taskbarList == null)
                {
                    lock (_syncLock)
                    {
                        if (_taskbarList == null)
                        {
                            _taskbarList = (ITaskbarList4)new CTaskbarList();
                            _taskbarList.HrInit();
                        }
                    }
                }

                return _taskbarList;
            }
        }
    }
}
