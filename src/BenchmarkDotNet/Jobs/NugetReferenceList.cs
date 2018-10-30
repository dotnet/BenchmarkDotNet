using System;
using System.Collections;
using System.Collections.Generic;

namespace BenchmarkDotNet.Jobs
{
    /// <summary>
    /// An ordered list of Nuget references. Does not allow duplicate references with the same PackageName.
    /// </summary>
    public class NugetReferenceList : IReadOnlyCollection<NugetReference>
    {
        private readonly List<NugetReference> references = new List<NugetReference>();

        public NugetReferenceList()
        {
        }

        public NugetReferenceList(IReadOnlyCollection<NugetReference> readOnlyCollection)
        {
            foreach (var nugetReference in readOnlyCollection)
            {
                Add(nugetReference);
            }
        }

        public int Count => references.Count;

        public IEnumerator<NugetReference> GetEnumerator() => references.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(NugetReference reference)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));

            var insertionIndex = references.BinarySearch(reference, PackageNameComparer.Default);
            if (0 <= insertionIndex && insertionIndex < Count)
                throw new ArgumentException($"Nuget package {reference.PackageName} was already added", nameof(reference));

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

            var otherList = (NugetReferenceList)obj;
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
            unchecked
            {
                int hashCode = 0;
                foreach (var nugetReference in references)
                {
                    hashCode = hashCode * 397 + nugetReference.GetHashCode();
                }

                return hashCode;
            }
        }

        private class PackageNameComparer : IComparer<NugetReference>
        {
            private PackageNameComparer() { }

            public static PackageNameComparer Default { get; } = new PackageNameComparer();

            public int Compare(NugetReference x, NugetReference y) => Comparer<string>.Default.Compare(x.PackageName, y.PackageName);
        }
    }
}