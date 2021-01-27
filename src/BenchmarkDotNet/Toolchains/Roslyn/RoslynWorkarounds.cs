using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    internal static class RoslynWorkarounds
    {
        internal static CSharpCompilationOptions WithIgnoreCorLibraryDuplicatedTypes(this CSharpCompilationOptions compilationOptions)
        {
            // this prevents from
            // The type 'ValueTuple<T1>' exists in both 'System.ValueTuple, Version=4.0.3.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' and 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
            // idea taken from: https://github.com/OmniSharp/omnisharp-roslyn/pull/785

            var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            var binderFlagsType = typeof(CSharpCompilationOptions).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.CSharp.BinderFlags");

            var ignoreCorLibraryDuplicatedTypesMember = binderFlagsType?.GetField("IgnoreCorLibraryDuplicatedTypes", BindingFlags.Static | BindingFlags.Public);
            var ignoreCorLibraryDuplicatedTypesValue = ignoreCorLibraryDuplicatedTypesMember?.GetValue(null);
            if (ignoreCorLibraryDuplicatedTypesValue != null)
            {
                topLevelBinderFlagsProperty?.SetValue(compilationOptions, ignoreCorLibraryDuplicatedTypesValue);
            }

            return compilationOptions;
        }
    }
}