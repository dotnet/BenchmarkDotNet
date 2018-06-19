using System;
using System.Collections.Generic;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Running
{
    internal class DescriptorComparer : IComparer<Descriptor>
    {
        public static readonly IComparer<Descriptor> Alphabetical = new DescriptorComparer(MethodOrderPolicy.Alphabetical);
        public static readonly IComparer<Descriptor> Declared = new DescriptorComparer(MethodOrderPolicy.Declared);

        private readonly MethodOrderPolicy methodOrderPolicy;

        public DescriptorComparer(MethodOrderPolicy methodOrderPolicy)
        {
            this.methodOrderPolicy = methodOrderPolicy;
        }

        public int Compare(Descriptor x, Descriptor y)
        {
            switch (methodOrderPolicy)
            {
                case MethodOrderPolicy.Alphabetical:
                    return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
                case MethodOrderPolicy.Declared:
                    return x.MethodIndex - y.MethodIndex;
                default:
                    throw new NotSupportedException($"methodOrderPolicy = {methodOrderPolicy}");
            }
        }
    }
}