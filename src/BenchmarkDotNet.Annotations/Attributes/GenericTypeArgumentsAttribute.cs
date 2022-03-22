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

        public GenericTypeArgumentsAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] params Type[] genericTypeArguments) => GenericTypeArguments = genericTypeArguments;
    }
}