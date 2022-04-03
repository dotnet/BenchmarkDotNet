using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GenericTypeArgumentsAttribute : Attribute
    {
        public Type[] GenericTypeArguments { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI] public GenericTypeArgumentsAttribute() => GenericTypeArguments = new Type[0];

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