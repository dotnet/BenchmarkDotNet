using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class CharacteristicObject<T> : CharacteristicObject
        where T : CharacteristicObject<T>, new()
    {
        protected CharacteristicObject() { }

        protected CharacteristicObject(string? id) : base(id) { }

        public new T Apply(CharacteristicObject other) => (T)ApplyCore(other);

        [PublicAPI]
        public T Apply(params CharacteristicObject[] others)
        {
            var result = this;
            foreach (var other in others)
            {
                result.Apply(other);
            }
            return (T)result;
        }

        [PublicAPI]
        public T ApplyAndFreeze(CharacteristicObject other) => Apply(other).Freeze();

        [PublicAPI]
        public T ApplyAndFreeze(params CharacteristicObject[] others) => Apply(others).Freeze();

        public new T Freeze() => (T)FreezeCore();

        public new T UnfreezeCopy() => (T)UnfreezeCopyCore();

        protected static Characteristic<TC> CreateCharacteristic<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] TC>(string memberName) => Characteristic.Create<T, TC>(memberName);

        protected static Characteristic<TC> CreateHiddenCharacteristic<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] TC>(string memberName) => Characteristic.CreateHidden<T, TC>(memberName);

        protected static Characteristic<TC> CreateIgnoreOnApplyCharacteristic<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] TC>(string memberName) => Characteristic.CreateIgnoreOnApply<T, TC>(memberName);
    }
}