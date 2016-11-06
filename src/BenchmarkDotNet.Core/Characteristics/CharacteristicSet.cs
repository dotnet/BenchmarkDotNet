using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Characteristics
{
    public sealed class CharacteristicSet : JobMode<CharacteristicSet>
    {
        public CharacteristicSet()
        {
            Apply();
        }

        public CharacteristicSet(JobMode other)
        {
            Apply(other);
        }

        public CharacteristicSet(params JobMode[] others)
        {
            Apply(others);
        }

        protected override bool IsPropertyBag => true;
    }
}