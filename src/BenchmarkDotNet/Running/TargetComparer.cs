using System;
using System.Collections.Generic;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Running
{
    internal class TargetComparer : IComparer<Target>
    {
        public static readonly IComparer<Target> Alphabetical = new TargetComparer(MethodOrderPolicy.Alphabetical);
        public static readonly IComparer<Target> Declared = new TargetComparer(MethodOrderPolicy.Declared);

        private readonly MethodOrderPolicy methodOrderPolicy;

        public TargetComparer(MethodOrderPolicy methodOrderPolicy)
        {
            this.methodOrderPolicy = methodOrderPolicy;
        }

        public int Compare(Target x, Target y)
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