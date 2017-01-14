using System;

namespace BenchmarkDotNet.Characteristics
{
    public class Characteristic<T> : Characteristic
    {
        internal Characteristic(
            string id,
            Type declaringType,
            Func<CharacteristicObject, T, T> resolver,
            T fallbackValue,
            bool ignoreOnApply)
            : base(id, typeof(T), declaringType, fallbackValue, ignoreOnApply)
        {
            Resolver = resolver;
            FallbackValue = fallbackValue;
        }

        public Func<CharacteristicObject, T, T> Resolver { get; }

        public new T FallbackValue { get; }

        public new T this[CharacteristicObject obj]
        {
            get { return obj.GetValue(this); }
            set { obj.SetValue(this, value); }
        }

        internal override object ResolveValueCore(CharacteristicObject obj, object currentValue)
        {
            if (Resolver == null)
                return (T)base.ResolveValueCore(obj, currentValue);

            return Resolver(obj, (T)base.ResolveValueCore(obj, currentValue));
        }
    }
}