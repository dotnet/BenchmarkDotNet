using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public static class GcModeExtensions
    {
        public static GcMode With<T>(this GcMode mode, ICharacteristic<T> characteristic) => GcMode.Parse(mode.ToSet().Mutate(characteristic));

        public static GcMode WithServer(this GcMode mode, bool value) => mode.With(mode.Server.Mutate(value));
        public static GcMode WithConcurrent(this GcMode mode, bool value) => mode.With(mode.Concurrent.Mutate(value));
        public static GcMode WithCpuGroups(this GcMode mode, bool value) => mode.With(mode.CpuGroups.Mutate(value));
        public static GcMode WithForce(this GcMode mode, bool value) => mode.With(mode.Force.Mutate(value));
        public static GcMode WithAllowVeryLargeObjects(this GcMode mode, bool value) => mode.With(mode.AllowVeryLargeObjects.Mutate(value));

    }
}