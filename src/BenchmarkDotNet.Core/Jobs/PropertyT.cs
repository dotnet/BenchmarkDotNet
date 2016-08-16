using System;

namespace BenchmarkDotNet.Jobs
{
    public struct Property<T> 
        where T : class, IProperty<T>, new()
    {
        // new T().CreateDefaultValue() is hack to overcome lack of generic constrains for static methods in C#
        private static readonly Lazy<T> DefaultValue = new Lazy<T>(() => new T().CreateDefaultValue()); 

        private readonly T value;

        private Property(T value)
        {
            this.value = value;
        }

        /// <summary>
        /// gets the value if set, otherwise returns default value
        /// </summary>
        public T Value => value ?? DefaultValue.Value; 

        public override string ToString() => value == null ? DefaultValue.Value.DefaultValueDisplayName : value.ToString();

        public static implicit operator Property<T>(T value) => new Property<T>(value);

        public static implicit operator T(Property<T> property) => property.Value;
    }

    public interface IProperty<T>
        where T : class, new()
    {
        T CreateDefaultValue();

        string DefaultValueDisplayName { get; }
    }
}