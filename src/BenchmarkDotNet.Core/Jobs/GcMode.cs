using System;
using System.Text;

namespace BenchmarkDotNet.Jobs
{
    // it should have been struct, but then I was not able to set up Concurrent = true as default value 
    // due to "cannot have instance property or field initializers in structs"
    public sealed class GcMode : IEquatable<GcMode>
    {
        /// <summary>
        /// Specifies whether the common language runtime runs server garbage collection.
        /// <value>false: Does not run server garbage collection. This is the default.</value>
        /// <value>true: Runs server garbage collection.</value>
        /// </summary>
        public bool Server { get; set; }

        /// <summary>
        /// Specifies whether the common language runtime runs garbage collection on a separate thread.
        /// <value>false: Does not run garbage collection concurrently.</value>
        /// <value>true: Runs garbage collection concurrently. This is the default.</value>
        /// </summary>
        public bool Concurrent { get; set; } = true;

        /// <summary>
        /// Specifies whether garbage collection supports multiple CPU groups.
        /// <value>false: Garbage collection does not support multiple CPU groups. This is the default.</value>
        /// <value>true: Garbage collection supports multiple CPU groups, if server garbage collection is enabled.</value>
        /// </summary>
        public bool CpuGroups { get; set; }

        /// <summary>
        /// Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
        /// <value>false: Does not force garbage collection.</value>
        /// <value>true: Forces full garbage collection after each benchmark invocation. This is the default.</value>
        /// </summary>
        public bool Force { get; set; } = true;

        /// <summary>
        /// On 64-bit platforms, enables arrays that are greater than 2 gigabytes (GB) in total size.
        /// <value>false: Arrays greater than 2 GB in total size are not enabled. This is the default.</value>
        /// <value>true: Arrays greater than 2 GB in total size are enabled on 64-bit platforms.</value>
        /// </summary>
        public bool AllowVeryLargeObjects { get; set; }

        /// <summary>
        /// the default settings "Concurrent = true, Server = false, CpuGroups = false, Force = true, AllowVeryLargeObjects = false"
        /// </summary>
        public static GcMode Default => new GcMode { Concurrent = true, Force = true };

        public override string ToString()
        {
            const string longestPossible = "DontForce Non-concurrent Workstation CpuGroupsEnabled AllowVeryLargeObjects";
            var representation = new StringBuilder(longestPossible.Length);

            if (Force == false) representation.Append("DontForce ");
            if (Concurrent) representation.Append("Concurrent ");
            if (Concurrent == false) representation.Append("Non-concurrent ");
            if (Server) representation.Append("Server");
            if (Server == false) representation.Append("Workstation");
            if (CpuGroups) representation.Append(" CpuGroupsEnabled");
            if (AllowVeryLargeObjects) representation.Append(" AllowVeryLargeObjects");

            return representation.ToString();
        }

        public bool Equals(GcMode other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Server == other.Server 
                && Concurrent == other.Concurrent 
                && CpuGroups == other.CpuGroups 
                && Force == other.Force
                && AllowVeryLargeObjects == other.AllowVeryLargeObjects;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is GcMode && Equals((GcMode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Server.GetHashCode();
                hashCode = (hashCode * 397) ^ Concurrent.GetHashCode();
                hashCode = (hashCode * 397) ^ CpuGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ Force.GetHashCode();
                hashCode = (hashCode * 397) ^ AllowVeryLargeObjects.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(GcMode left, GcMode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GcMode left, GcMode right)
        {
            return !Equals(left, right);
        }
    }
}