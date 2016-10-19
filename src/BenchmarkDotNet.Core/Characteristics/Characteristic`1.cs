using System;

namespace BenchmarkDotNet.Characteristics
{
    public class Characteristic<T> : Characteristic
    {
        internal Characteristic(
            string id,
            Type declaringType,
            Func<JobMode, T, T> resolver,
            T fallbackValue,
            bool dontClone)
            : base(id, typeof(T), declaringType, fallbackValue, dontClone)
        {
            Resolver = resolver;
            FallbackValue = fallbackValue;
        }

        public Func<JobMode, T, T> Resolver { get; }

        public new T FallbackValue { get; }

        public new T this[JobMode obj]
        {
            get { return obj.GetValue(this); }
            set { obj.SetValue(this, value); }
        }

        internal override object ResolveValueCore(JobMode obj, object currentValue)
        {
            if (Resolver == null)
                return (T)base.ResolveValueCore(obj, currentValue);

            return Resolver(obj, (T)base.ResolveValueCore(obj, currentValue));
        }
    }
}