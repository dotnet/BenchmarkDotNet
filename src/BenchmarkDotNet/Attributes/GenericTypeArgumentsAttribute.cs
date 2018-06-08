using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GenericTypeArgumentsAttribute : Attribute
    {
        public Type[] GenericTypeArguments { get; }

        // CLS-Compliant Code requires a constuctor without an array in the argument list
        public GenericTypeArgumentsAttribute() => GenericTypeArguments = Array.Empty<Type>();

        public GenericTypeArgumentsAttribute(params Type[] genericTypeArguments) => GenericTypeArguments = genericTypeArguments;
    }
}