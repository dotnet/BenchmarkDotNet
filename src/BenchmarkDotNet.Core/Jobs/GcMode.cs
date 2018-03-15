﻿using System;
using BenchmarkDotNet.Characteristics;

// ReSharper disable once CheckNamespace
namespace BenchmarkDotNet.Jobs
{
    public sealed class GcMode : JobMode<GcMode>
    {
        public static readonly Characteristic<bool> ServerCharacteristic = Characteristic.Create((GcMode g) => g.Server);
        public static readonly Characteristic<bool> ConcurrentCharacteristic = Characteristic.Create((GcMode g) => g.Concurrent);
        public static readonly Characteristic<bool> CpuGroupsCharacteristic = Characteristic.Create((GcMode g) => g.CpuGroups);
        public static readonly Characteristic<bool> ForceCharacteristic = Characteristic.Create((GcMode g) => g.Force);
        public static readonly Characteristic<bool> AllowVeryLargeObjectsCharacteristic = Characteristic.Create((GcMode g) => g.AllowVeryLargeObjects);
        public static readonly Characteristic<bool> RetainVmCharacteristic = Characteristic.Create((GcMode g) => g.RetainVm);
        public static readonly Characteristic<bool> NoAffinitizeCharacteristic = Characteristic.Create((GcMode g) => g.NoAffinitize);
        public static readonly Characteristic<int> HeapAffinitizeMaskCharacteristic = Characteristic.Create((GcMode g) => g.HeapAffinitizeMask);
        public static readonly Characteristic<int> HeapCountCharacteristic = Characteristic.Create((GcMode g) => g.HeapCount);

        /// <summary>
        /// Specifies whether the common language runtime runs server garbage collection.
        /// <value>false: Does not run server garbage collection. This is the default.</value>
        /// <value>true: Runs server garbage collection.</value>
        /// </summary>
        public bool Server
        {
            get { return ServerCharacteristic[this]; }
            set { ServerCharacteristic[this] = value; }
        }

        /// <summary>
        /// Specifies whether the common language runtime runs garbage collection on a separate thread.
        /// <value>false: Does not run garbage collection concurrently.</value>
        /// <value>true: Runs garbage collection concurrently. This is the default.</value>
        /// </summary>
        public bool Concurrent
        {
            get { return ConcurrentCharacteristic[this]; }
            set { ConcurrentCharacteristic[this] = value; }
        }

        /// <summary>
        /// Specifies whether garbage collection supports multiple CPU groups.
        /// <value>false: Garbage collection does not support multiple CPU groups. This is the default.</value>
        /// <value>true: Garbage collection supports multiple CPU groups, if server garbage collection is enabled.</value>
        /// </summary>
        public bool CpuGroups
        {
            get { return CpuGroupsCharacteristic[this]; }
            set { CpuGroupsCharacteristic[this] = value; }
        }

        /// <summary>
        /// Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
        /// <value>false: Does not force garbage collection.</value>
        /// <value>true: Forces full garbage collection after each benchmark invocation. This is the default.</value>
        /// </summary>
        public bool Force
        {
            get { return ForceCharacteristic[this]; }
            set { ForceCharacteristic[this] = value; }
        }

        /// <summary>
        /// On 64-bit platforms, enables arrays that are greater than 2 gigabytes (GB) in total size.
        /// <value>false: Arrays greater than 2 GB in total size are not enabled. This is the default.</value>
        /// <value>true: Arrays greater than 2 GB in total size are enabled on 64-bit platforms.</value>
        /// </summary>
        public bool AllowVeryLargeObjects
        {
            get { return AllowVeryLargeObjectsCharacteristic[this]; }
            set { AllowVeryLargeObjectsCharacteristic[this] = value; }
        }

        /// <summary>
        /// Put segments that should be deleted on a standby list for future use instead of releasing them back to the OS
        /// <remarks>The default is false</remarks>
        /// </summary>
        public bool RetainVm
        {
            get { return RetainVmCharacteristic[this]; }
            set { RetainVmCharacteristic[this] = value; }
        }

        /// <summary>
        /// specify true to disable hard affinity of Server GC threads to CPUs
        /// </summary>
        public bool NoAffinitize
        {
            get { return NoAffinitizeCharacteristic[this]; }
            set { NoAffinitizeCharacteristic[this] = value; }
        }

        /// <summary>
        /// process mask, see <see href="https://support.microsoft.com/en-us/help/4014604/may-2017-description-of-the-quality-rollup-for-the-net-framework-4-6-4">MSDN</see> for more.
        /// </summary>
        public int HeapAffinitizeMask
        {
            get { return HeapAffinitizeMaskCharacteristic[this]; }
            set { HeapAffinitizeMaskCharacteristic[this] = value; }
        }

        /// <summary>
        ///  specify the # of Server GC threads/heaps, must be smaller than the # of logical CPUs the process is allowed to run on, 
        ///  ie, if you don't specifically affinitize your process it means the # of total logical CPUs on the machine; 
        ///  otherwise this is the # of logical CPUs you affinitized your process to.
        /// </summary>
        public int HeapCount
        {
            get { return HeapCountCharacteristic[this]; }
            set { HeapCountCharacteristic[this] = value; }
        }
    }
}