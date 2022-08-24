using System;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Characteristics
{
    public class Characteristic<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T> : Characteristic
    {
        internal Characteristic(
            string id,
            Type declaringType,
            Func<CharacteristicObject, T, T> resolver,
            T fallbackValue,
            bool ignoreOnApply,
            bool dontShowInSummary = false)
            : base(id, typeof(T), declaringType, fallbackValue, ignoreOnApply, dontShowInSummary)
        {
            Resolver = resolver;
            FallbackValue = fallbackValue;
        }

        private Func<CharacteristicObject, T, T> Resolver { get; }

        public T FallbackValue { get; }

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