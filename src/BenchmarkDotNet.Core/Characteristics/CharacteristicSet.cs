using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Characteristics
{
    public sealed class CharacteristicSet : JobMode<CharacteristicSet>
    {
        protected override bool IsPropertyBag => true;
    }
}