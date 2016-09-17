using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Characteristics
{
    public class CharacteristicSet
    {
        private readonly Dictionary<string, ICharacteristic> characteristics = new Dictionary<string, ICharacteristic>();

        public CharacteristicSet(params ICharacteristic[] characteristics)
        {
            foreach (var characteristic in characteristics.Where(c => c != null))
                this.characteristics[characteristic.Id] = characteristic;
        }

        private CharacteristicSet(params CharacteristicSet[] sets)
        {
            Mutate(sets);
        }

        public CharacteristicSet Mutate(params CharacteristicSet[] sets)
        {
            foreach (var set in sets)
                foreach (var characteristic in set.GetValues())
                    characteristics[characteristic.Id] = characteristic;
            return this;
        }

        public CharacteristicSet Mutate<T>(ICharacteristic<T> characteristic)
        {
            characteristics[characteristic.Id] = characteristic;
            return this;
        }

        public ICharacteristic[] GetValues() => characteristics.Values.ToArray();

        public ICharacteristic Get(string id) => characteristics.GetValueOrDefault(id);

        public static CharacteristicSet Merge(params CharacteristicSet[] sets) => new CharacteristicSet(sets);

    }
}