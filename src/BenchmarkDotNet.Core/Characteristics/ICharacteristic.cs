using System;

namespace BenchmarkDotNet.Characteristics
{
    public interface ICharacteristic
    {
        string Id { get; }
        bool IsDefault { get; }
        object ObjectValue { get; }
        ICharacteristic Mutate(object value);
    }

    public interface ICharacteristic<T> : ICharacteristic
    {
        T SpecifiedValue { get; }

        ICharacteristic<T> MakeDefault();

        ICharacteristic<T> Mutate(T value);

        /// <summary>
        /// If <paramref name="set"/> contains a value with current Id, the generic version of the caracteristic will be returned.
        /// Otherwise, original value will be returned.
        /// </summary>
        /// <exception cref="InvalidOperationException">If CharacteristicSet an element with requried Id and wrong type, InvalidOperationException will be trown</exception>
        ICharacteristic<T> Mutate(CharacteristicSet set);
    }
}