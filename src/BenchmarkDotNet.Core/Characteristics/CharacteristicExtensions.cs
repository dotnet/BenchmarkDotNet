using System.Linq;

namespace BenchmarkDotNet.Characteristics
{
    public static class CharacteristicExtensions
    {
        public static T Resolve<T>(this ICharacteristic<T> characteristic, IResolver resolver) => resolver.Resolve(characteristic);

        public static string GetDisplayId(this ICharacteristic characteristic) => CharacteristicHelper.GetDisplayName(characteristic.Id);

        public static bool AllAreDefaults(this CharacteristicSet set) => set.GetValues().All(c => c.IsDefault);
    }
}