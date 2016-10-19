using System;
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
    }
}