using System;
using System.Diagnostics.CodeAnalysis;
using static BenchmarkDotNet.Characteristics.CharacteristicHelper;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class Characteristic
    {
        public static readonly object EmptyValue = new object();

        public static Characteristic<T> Create<TOwner, [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(string memberName)
            where TOwner : CharacteristicObject
            => new Characteristic<T>(
                memberName,
                typeof(TOwner),
                null, default,
                false);

        public static Characteristic<T> Create<TOwner, [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(string memberName, T fallbackValue)
            where TOwner : CharacteristicObject
            => new Characteristic<T>(
                memberName,
                typeof(TOwner),
                null, fallbackValue,
                false);

        public static Characteristic<T> Create<TOwner, [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(string memberName, Func<CharacteristicObject, T, T> resolver, T fallbackValue, bool ignoreOnApply)
            where TOwner : CharacteristicObject
            => new Characteristic<T>(
                memberName,
                typeof(TOwner),
                resolver, fallbackValue,
                ignoreOnApply);

        public static Characteristic<T> CreateHidden<TOwner, [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(string memberName)
            where TOwner : CharacteristicObject
            => new Characteristic<T>(
                memberName,
                typeof(TOwner),
                null, default,
                false, true);

        public static Characteristic<T> CreateIgnoreOnApply<TOwner, [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(string memberName)
            where TOwner : CharacteristicObject
            => new Characteristic<T>(
                memberName,
                typeof(TOwner),
                null, default,
                true);

        protected Characteristic(
            string id,
            [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] Type characteristicType,
            Type declaringType,
            object fallbackValue,
            bool ignoreOnApply,
            bool dontShowInSummary = false)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            if (characteristicType == null)
                throw new ArgumentNullException(nameof(characteristicType));
            if (declaringType == null)
                throw new ArgumentNullException(nameof(declaringType));

            Id = id;
            CharacteristicType = characteristicType;
            DeclaringType = declaringType;
            FallbackValue = fallbackValue;
            IgnoreOnApply = ignoreOnApply;
            DontShowInSummary = dontShowInSummary;
        }

        public string Id { get; }
        public string FullId => DeclaringType.Name + "." + Id;

        // TODO: better naming. As it is for now this property used for Id only and has meaning "if set, will not transfer to others nor be cleared".
        public bool IgnoreOnApply { get; }

        public bool DontShowInSummary { get; }

        [DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)]
        public Type CharacteristicType { get; }

        public Type DeclaringType { get; }

        private object FallbackValue { get; }

        public object this[CharacteristicObject obj]
        {
            get { return obj.GetValue(this); }
            set { obj.SetValue(this, value); }
        }

        public bool HasChildCharacteristics => IsCharacteristicObjectSubclass(CharacteristicType);

        internal virtual object ResolveValueCore(CharacteristicObject obj, object currentValue) =>
            ReferenceEquals(currentValue, EmptyValue) ? FallbackValue : currentValue;

        public override string ToString() => Id;
    }
}