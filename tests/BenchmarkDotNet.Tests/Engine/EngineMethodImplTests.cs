using System;
using System.Reflection;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine;

public class EngineMethodImplTests
{
    [Fact]
    public void AllEngineMethodsAreAggressivelyOptimized()
    {
        AssertMethodsAggressivelyOptimized(typeof(Engines.Engine));

        static void AssertMethodsAggressivelyOptimized(Type type)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                Assert.True(
                    method.MethodImplementationFlags.HasFlag(BenchmarkDotNet.Portability.CodeGenHelper.AggressiveOptimizationOptionForEmit),
                    $"Method is not aggressively optimized: {method}"
                );
            }

            foreach (var nestedType in type.GetNestedTypes())
            {
                AssertMethodsAggressivelyOptimized(nestedType);
            }
        }
    }
}
