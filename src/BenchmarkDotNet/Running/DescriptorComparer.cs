using System;
using System.Collections.Generic;
using BenchmarkDotNet.Order;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    internal class MethodComparer : IComparer<Descriptor>
    {
        [PublicAPI] public static readonly IComparer<Descriptor> Alphabetical = new MethodComparer(MethodOrderPolicy.Alphabetical);
        [PublicAPI] public static readonly IComparer<Descriptor> Declared = new MethodComparer(MethodOrderPolicy.Declared);

        private readonly MethodOrderPolicy methodOrderPolicy;

        public MethodComparer(MethodOrderPolicy methodOrderPolicy)
        {
            this.methodOrderPolicy = methodOrderPolicy;
        }

        public int Compare(Descriptor x, Descriptor y)
        {
            if (x == null && y == null) return 0;
            if (x != null && y == null) return 1;
            if (x == null) return -1;
            switch (methodOrderPolicy)
            {
                case MethodOrderPolicy.Alphabetical:
                    return string.CompareOrdinal(x.WorkloadMethodDisplayInfo, y.WorkloadMethodDisplayInfo);
                case MethodOrderPolicy.Declared:
                    return x.MethodIndex - y.MethodIndex;
                default:
                    throw new NotSupportedException($"methodOrderPolicy = {methodOrderPolicy}");
            }
        }
    }
}