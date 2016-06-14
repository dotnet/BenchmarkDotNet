using System;
using System.Text;

namespace BenchmarkDotNet.Jobs
{
    // it should have been struct, but then I was not able to set up Concurrent = true as default value 
    // due to "cannot have instance property or field initializers in structs"
    public sealed class GC : IEquatable<GC>
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
        /// the default settings in .NET are "Concurrent = true, Server = false, CpuGroups = false"
        /// </summary>
        public static GC Default => new GC { Concurrent = true };

        public override string ToString()
        {
            const string longestPossible = "Nonconcurrent Workstation CpuGroupsEnabled";
            var representation = new StringBuilder(longestPossible.Length);

            if (Concurrent) representation.Append("Concurrent");
            if (Concurrent == false) representation.Append("Nonconcurrent");
            if (Server) representation.Append(" Server");
            if (Server == false) representation.Append(" Workstation");
            if(CpuGroups) representation.Append(" CpuGroupsEnabled");

            return representation.ToString();
        }

        public bool Equals(GC other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Server == other.Server && Concurrent == other.Concurrent && CpuGroups == other.CpuGroups;
        }

        public static bool operator ==(GC left, GC right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GC left, GC right)
        {
            return !Equals(left, right);
        }
    }
}