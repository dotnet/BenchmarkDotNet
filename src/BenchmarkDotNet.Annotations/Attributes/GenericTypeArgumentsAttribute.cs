using System;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GenericTypeArgumentsAttribute : Attribute
    {
        public Type[] GenericTypeArguments { get; }

        public GenericTypeArgumentsAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
            => GenericTypeArguments = new Type[] { type };

        public GenericTypeArgumentsAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type2)
            => GenericTypeArguments = new Type[] { type1, type2 };

        public GenericTypeArgumentsAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type3)
            => GenericTypeArguments = new Type[] { type1, type2, type3 };
    }
}