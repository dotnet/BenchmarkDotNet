using System;

namespace BenchmarkDotNet.Characteristics
{
    public class Characteristic<T> : ICharacteristic<T>
    {
        public static Characteristic<T> Create(params string[] id) => new Characteristic<T>(id);

        public string Id { get; }
        public bool IsDefault { get; }

        private readonly T specifiedValue;

        public T SpecifiedValue
        {
            get
            {
                if (IsDefault)
                    throw new InvalidOperationException("There is no a specified value for default");
                return specifiedValue;
            }
        }

        public object ObjectValue => SpecifiedValue;

        private Characteristic(params string[] id)
        {
            Id = string.Join(CharacteristicHelper.IdSeparator, id);
            IsDefault = true;
        }

        private Characteristic(string id, T specifiedValue)
        {
            Id = id;
            IsDefault = false;
            this.specifiedValue = specifiedValue;
        }

        public ICharacteristic<T> MakeDefault() => new Characteristic<T>(Id);

        public ICharacteristic<T> Mutate(T value) => new Characteristic<T>(Id, value);

        public ICharacteristic Mutate(object value)
        {
            if (value is T)
                return Mutate((T) value);
            throw new ArgumentException($"value should be an instance of {typeof(T).FullName}", nameof(value));
        }

        public ICharacteristic<T> Mutate(CharacteristicSet set)
        {
            var generic = set.Get(Id);
            if (generic != null)
            {
                if (generic.IsDefault)
                    return Create(Id);
                var value = generic.ObjectValue;
                if (value is T)
                    return Mutate((T) value);
                throw new InvalidOperationException($"CharacteristicSet contains an element with id = {Id} but the value isn't an instance of {typeof(T).Name}");
            }
            return this;
        }
    }
}