using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Jobs
{
    public sealed class GcMode
    {
        public static readonly GcMode Default = new GcMode();

        private GcMode()
        {
        }

        private static ICharacteristic<T> Create<T>(string id) => Characteristic<T>.Create("Env", "Gc" + id);

        /// <summary>
        /// Specifies whether the common language runtime runs server garbage collection.
        /// <value>false: Does not run server garbage collection. This is the default.</value>
        /// <value>true: Runs server garbage collection.</value>
        /// </summary>
        public ICharacteristic<bool> Server { get; private set; } = Create<bool>(nameof(Server));

        /// <summary>
        /// Specifies whether the common language runtime runs garbage collection on a separate thread.
        /// <value>false: Does not run garbage collection concurrently.</value>
        /// <value>true: Runs garbage collection concurrently. This is the default.</value>
        /// </summary>
        public ICharacteristic<bool> Concurrent { get; private set; } = Create<bool>(nameof(Concurrent));

        /// <summary>
        /// Specifies whether garbage collection supports multiple CPU groups.
        /// <value>false: Garbage collection does not support multiple CPU groups. This is the default.</value>
        /// <value>true: Garbage collection supports multiple CPU groups, if server garbage collection is enabled.</value>
        /// </summary>
        public ICharacteristic<bool> CpuGroups { get; private set; } = Create<bool>(nameof(CpuGroups));

        /// <summary>
        /// Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
        /// <value>false: Does not force garbage collection.</value>
        /// <value>true: Forces full garbage collection after each benchmark invocation. This is the default.</value>
        /// </summary>
        public ICharacteristic<bool> Force { get; private set; } = Create<bool>(nameof(Force));

        /// <summary>
        /// On 64-bit platforms, enables arrays that are greater than 2 gigabytes (GB) in total size.
        /// <value>false: Arrays greater than 2 GB in total size are not enabled. This is the default.</value>
        /// <value>true: Arrays greater than 2 GB in total size are enabled on 64-bit platforms.</value>
        /// </summary>
        public ICharacteristic<bool> AllowVeryLargeObjects { get; private set; } = Create<bool>(nameof(AllowVeryLargeObjects));

        public static GcMode Parse(CharacteristicSet set)
        {
            var mode = new GcMode();
            mode.Server = mode.Server.Mutate(set);
            mode.Concurrent = mode.Concurrent.Mutate(set);
            mode.CpuGroups = mode.CpuGroups.Mutate(set);
            mode.Force = mode.Force.Mutate(set);
            mode.AllowVeryLargeObjects = mode.AllowVeryLargeObjects.Mutate(set);
            return mode;
        }

        public CharacteristicSet ToSet() => new CharacteristicSet(
            Server,
            Concurrent,
            CpuGroups,
            Force,
            AllowVeryLargeObjects
        );

        public JobMutator ToMutator() => new JobMutator("SpecificGcMode").Add(ToSet());
    }
}