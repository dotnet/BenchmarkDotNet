using System;
using System.Collections;
using System.Collections.Generic;

namespace BenchmarkDotNet.Jobs
{
    /// <summary>
    /// An ordered list of NuGet references. Does not allow duplicate references with the same PackageName.
    /// </summary>
    public class NuGetReferenceList : IReadOnlyCollection<NuGetReference>
    {
        private readonly List<NuGetReference> references = new List<NuGetReference>();

        public NuGetReferenceList()
        {
        }

        public NuGetReferenceList(IReadOnlyCollection<NuGetReference> readOnlyCollection)
        {
            foreach (var nuGetReference in readOnlyCollection)
            {
                Add(nuGetReference);
            }
        }

        public int Count => references.Count;

        public IEnumerator<NuGetReference> GetEnumerator() => references.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(NuGetReference reference)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));

            var insertionIndex = references.BinarySearch(reference, PackageNameComparer.Default);
            if (0 <= insertionIndex && insertionIndex < Count)
                throw new ArgumentException($"NuGet package {reference.PackageName} was already added", nameof(reference));

            references.Insert(~insertionIndex, reference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            var otherList = (NuGetReferenceList)obj;
            if (Count != otherList.Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (!references[i].Equals(otherList.references[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            foreach (var reference in references)
                hashCode.Add(reference);
            return hashCode.ToHashCode();
        }

        private class PackageNameComparer : IComparer<NuGetReference>
        {
            private PackageNameComparer() { }

            public static PackageNameComparer Default { get; } = new PackageNameComparer();

            public int Compare(NuGetReference x, NuGetReference y) => Comparer<string>.Default.Compare(x.PackageName, y.PackageName);
        }
    }
}