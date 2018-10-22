using BenchmarkDotNet.Characteristics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public abstract class JobMode<T> : CharacteristicObject<T> where T : JobMode<T>, new()
    {
        public static readonly T Default = new T().Freeze();

        protected JobMode() { }

        protected JobMode(string id) : base(id) { }

        [PublicAPI] public Job Job => OwnerOrSelf as Job;
    }
}