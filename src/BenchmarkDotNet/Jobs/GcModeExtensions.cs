using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public static class GcModeExtensions
    {
        /// <summary>
        /// Specifies whether the common language runtime runs server garbage collection.
        /// <value>false: Does not run server garbage collection. This is the default.</value>
        /// <value>true: Runs server garbage collection.</value>
        /// </summary>
        [PublicAPI]
        public static GcMode WithServer(this GcMode mode, bool value) => mode.WithCore(m => m.Server = value);

        /// <summary>
        /// Specifies whether the common language runtime runs garbage collection on a separate thread.
        /// <value>false: Does not run garbage collection concurrently.</value>
        /// <value>true: Runs garbage collection concurrently. This is the default.</value>
        /// </summary>
        [PublicAPI]
        public static GcMode WithConcurrent(this GcMode mode, bool value) => mode.WithCore(m => m.Concurrent = value);

        /// <summary>
        /// Specifies whether garbage collection supports multiple CPU groups.
        /// <value>false: Garbage collection does not support multiple CPU groups. This is the default.</value>
        /// <value>true: Garbage collection supports multiple CPU groups, if server garbage collection is enabled.</value>
        /// </summary>
        [PublicAPI]
        public static GcMode WithCpuGroups(this GcMode mode, bool value) => mode.WithCore(m => m.CpuGroups = value);

        /// <summary>
        /// Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
        /// <value>false: Does not force garbage collection.</value>
        /// <value>true: Forces full garbage collection after each benchmark invocation. This is the default.</value>
        /// </summary>
        [PublicAPI]
        public static GcMode WithForce(this GcMode mode, bool value) => mode.WithCore(m => m.Force = value);

        /// <summary>
        /// On 64-bit platforms, enables arrays that are greater than 2 gigabytes (GB) in total size.
        /// <value>false: Arrays greater than 2 GB in total size are not enabled. This is the default.</value>
        /// <value>true: Arrays greater than 2 GB in total size are enabled on 64-bit platforms.</value>
        /// </summary>
        [PublicAPI]
        public static GcMode WithAllowVeryLargeObjects(this GcMode mode, bool value) => mode.WithCore(m => m.AllowVeryLargeObjects = value);

        /// <summary>
        /// Put segments that should be deleted on a standby list for future use instead of releasing them back to the OS
        /// <remarks>The default is false</remarks>
        /// </summary>
        [PublicAPI]
        public static GcMode WithRetainVm(this GcMode mode, bool value) => mode.WithCore(m => m.RetainVm = value);

        /// <summary>
        ///  specify the # of Server GC threads/heaps, must be smaller than the # of logical CPUs the process is allowed to run on,
        ///  ie, if you don't specifically affinitize your process it means the # of total logical CPUs on the machine;
        ///  otherwise this is the # of logical CPUs you affinitized your process to.
        /// </summary>
        [PublicAPI]
        public static GcMode WithHeapCount(this GcMode mode, int heapCount) => mode.WithCore(m => m.HeapCount = heapCount);

        /// <summary>
        /// specify true to disable hard affinity of Server GC threads to CPUs
        /// </summary>
        [PublicAPI]
        public static GcMode WithNoAffinitize(this GcMode mode, bool value) => mode.WithCore(m => m.NoAffinitize = value);

        /// <summary>
        /// process mask, see <see href="https://support.microsoft.com/en-us/help/4014604/may-2017-description-of-the-quality-rollup-for-the-net-framework-4-6-4">MSDN</see> for more.
        /// </summary>
        [PublicAPI]
        public static GcMode WithHeapAffinitizeMask(this GcMode mode, int heapAffinitizeMask) => mode.WithCore(m => m.HeapAffinitizeMask = heapAffinitizeMask);

        private static GcMode WithCore(this GcMode mode, Action<GcMode> updateCallback)
        {
            mode = new GcMode().Apply(mode);
            updateCallback(mode);
            return mode;
        }
    }
}