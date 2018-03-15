using System;
using System.Linq.Expressions;

using static BenchmarkDotNet.Characteristics.CharacteristicHelper;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class Characteristic
    {
        public static readonly object EmptyValue = new object();

        #region Factory methods
        public static Characteristic<T> Create<TOwner, T>(
            Expression<Func<TOwner, T>> propertyGetterExpression)
            where TOwner : CharacteristicObject =>
            new Characteristic<T>(
                GetMemberName(propertyGetterExpression),
                GetDeclaringType(propertyGetterExpression),
                null, default,
                false);

        public static Characteristic<T> Create<TOwner, T>(
            Expression<Func<TOwner, T>> propertyGetterExpression,
            T fallbackValue)
            where TOwner : CharacteristicObject =>
            new Characteristic<T>(
                GetMemberName(propertyGetterExpression),
                GetDeclaringType(propertyGetterExpression),
                null, fallbackValue,
                false);

        public static Characteristic<T> Create<TOwner, T>(
            Expression<Func<TOwner, T>> propertyGetterExpression,
            Func<CharacteristicObject, T, T> resolver,
            T fallbackValue)
            where TOwner : CharacteristicObject =>
            new Characteristic<T>(
                GetMemberName(propertyGetterExpression),
                GetDeclaringType(propertyGetterExpression),
                resolver, fallbackValue,
                false);

        public static Characteristic<T> Create<TOwner, T>(
            Expression<Func<TOwner, T>> propertyGetterExpression,
            Func<CharacteristicObject, T, T> resolver,
            T fallbackValue,
            bool ignoreOnApply)
            where TOwner : CharacteristicObject =>
            new Characteristic<T>(
                GetMemberName(propertyGetterExpression),
                GetDeclaringType(propertyGetterExpression),
                resolver, fallbackValue,
                ignoreOnApply);
        #endregion

        protected Characteristic(
            string id,
            Type characteristicType,
            Type declaringType,
            object fallbackValue,
            bool ignoreOnApply)
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
        }

        public string Id { get; }
        public string FullId => DeclaringType.Name + "." + Id;

        // TODO: better naming. As it is for now this property used for Id only and has meaning "if set, will not transfer to others nor be cleared".
        public bool IgnoreOnApply { get; }

        public Type CharacteristicType { get; }

        public Type DeclaringType { get; }

        public object FallbackValue { get; }

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