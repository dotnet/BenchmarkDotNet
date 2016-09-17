using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public class JobMutator
    {
        private readonly CharacteristicSet characteristicSet = new CharacteristicSet();

        public string Id { get; }

        public JobMutator(string id)
        {
            Id = id;
        }

        public JobMutator Add<T>(ICharacteristic<T> characteristic)
        {
            characteristicSet.Mutate(characteristic);
            return this;
        }

        public JobMutator Add(CharacteristicSet set)
        {
            characteristicSet.Mutate(set);
            return this;
        }

        public Job Apply(Job job)
        {
            string jobId = job.ResolvedId + "-" + Id;
            return Job.Parse(job.ToSet().Mutate(characteristicSet)).WithId(jobId);
        }
    }
}