using System;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Attributes.CompilerServices;

// MethodImplOptions.AggressiveOptimization is applied to all methods to force them to go straight to tier1 JIT,
// eliminating tiered JIT as a potential variable in measurements.
// This is necessary because C# does not support any way to apply attributes to compiler-generated state machine methods.
// This is applied both to the core Engine and auto-generated classes.
#pragma warning disable CS1574
/// <summary>
/// Instructs the BenchmarkDotNet assembly weaver to apply <see cref="MethodImplOptions.AggressiveOptimization"/> to all declared
/// methods in the annotated type and nested types that are not already annotated with <see cref="MethodImplOptions.NoOptimization"/>.
/// </summary>
#pragma warning restore CS1574
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class AggressivelyOptimizeMethodsAttribute : Attribute
{
}