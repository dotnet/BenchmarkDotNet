using System;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public abstract class JobMode<T> : CharacteristicObject<T> where T : JobMode<T>, new()
    {
        public static readonly T Default = new T().Freeze();

        protected JobMode() { }

        protected JobMode(string id) : base(id) { }

        public Job Job => OwnerOrSelf as Job;
    }
}