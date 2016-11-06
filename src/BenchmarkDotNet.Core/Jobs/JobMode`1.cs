using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public abstract class JobMode<T> : JobMode where T : JobMode<T>, new()
    {
        public static readonly T Default = new T().Freeze();

        protected JobMode() { }

        protected JobMode(string id) : base(id) { }

        public Job Job => OwnerOrSelf as Job;

        public new T Apply(JobMode other) => (T)ApplyCore(other);

        public T Apply(params JobMode[] others)
        {
            var result = this;
            foreach (var other in others)
            {
                result.Apply(other);
            }
            return (T)result;
        }

        public T ApplyAndFreeze(JobMode other) => Apply(other).Freeze();

        public T ApplyAndFreeze(params JobMode[] others) => Apply(others).Freeze();

        public new T Freeze() => (T)FreezeCore();

        public new T UnfreezeCopy() => (T)UnfreezeCopyCore();
    }
}